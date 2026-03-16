#nullable disable
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs._Cache;
using TaxiDispatch.DTOs.User;
using TaxiDispatch.DTOs.User.Responses;
using TaxiDispatch.Services.Cache;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Data;
using System.Text;

namespace TaxiDispatch.Services
{
    public class UserProfileService : BaseService<UserProfileService>
    {

        private readonly AceMessagingService _messagingService;
        private readonly TaxiDispatchContext _dB;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserLocationCacheService _cacheLocation;
        private readonly UserProfileCacheService _cacheProfile;


        public UserProfileService(
            TaxiDispatchContext dB,
            IMapper mapper,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            AceMessagingService messagingService,
            ILogger<UserProfileService> logger,
            UserLocationCacheService cacheLocation,
            UserProfileCacheService cacheProfile)
            : base(dB, logger)
        {
            _dB = dB;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _messagingService = messagingService;
            _cacheLocation = cacheLocation;
            _cacheProfile = cacheProfile;
        }

        public async Task<IdentityResult> Create(AppUser newUser, string password, UserProfile profile)
        {
            var result = await _userManager.CreateAsync(newUser, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Driver");

                await LockoutOnOff(newUser.Id, false);

                profile.UserId = newUser.Id;

                await _dB.UserProfiles.AddAsync(profile);
                var res = await _dB.SaveChangesAsync();

                // send welcome email
                await _messagingService.SendRegistrationEmail(newUser.Email, newUser.FullName,
                    new DTOs.MessageTemplates.NewUserRegisteredDto { 
                        fullname = newUser.FullName, password = password, reg = profile.RegNo, 
                        userid = newUser.Id, username = newUser.UserName});
            }

            return result;
        }

        #region Account User
        public async Task<IdentityResult> CreateAccountUser(AppUser newUser, string password, int accno)
        {
            var result = await _userManager.CreateAsync(newUser, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Account");

                await LockoutOnOff(newUser.Id, false);

                // send welcome email
                await _messagingService.SendAccountRegistrationEmail(newUser.Email, newUser.FullName,
                    new DTOs.MessageTemplates.NewUserRegisteredDto
                    {
                        fullname = newUser.FullName,
                        password = password,
                        accno = accno,
                        userid = newUser.Id,
                        username = newUser.UserName
                    });
            }

            return result;
        }
        #endregion

        public async Task UpdateLastLoginDateTime(int userId)
        {
            await _dB.UserProfiles.Where(o => o.UserId == userId)
                      .ExecuteUpdateAsync(b => b.SetProperty(u => u.LastLogin, DateTime.Now));

            await _dB.Set<TenantUser>().Where(o => o.UserId == userId)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(u => u.LastLoginUtc, DateTime.UtcNow)
                    .SetProperty(u => u.UpdatedUtc, DateTime.UtcNow));
        }

        public async Task AddUserToRole(AppUser newUser, string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            _dB.Database.ExecuteSql($"INSERT INTO [dbo].[AspNetUserRoles] ([UserId],[RoleId]) VALUES ({newUser.Id}, {role.Id})");
        }

        public async Task<string> GetEmail(int userid)
        {
            return await _dB.Users.Where(o => o.Id == userid).Select(o => o.Email).FirstOrDefaultAsync();
        }

        public async Task ChangePassword(ListedUser user)
        {
            var res = await _userManager.FindByEmailAsync(user.Email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(res);

            var password = $"ace{DateTime.Now.Hour}{DateTime.Now.Minute}";

            await _userManager.ResetPasswordAsync(res, token, password);
            await _messagingService.SendEmailAsync(user.Email,"Password Reset",$"Your password has been reset.\r\n\r\nYour new password is {password}");
        }

        public async Task ChangePassword(int userId)
        {
            var email = await GetEmail(userId);

            var res = await _userManager.FindByEmailAsync(email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(res);

            var password = $"ace{DateTime.Now.Hour}{DateTime.Now.Minute}";

            await _userManager.ResetPasswordAsync(res, token, password);
            await _messagingService.SendEmailAsync(email, "Password Reset", $"Your password has been reset.\r\n\r\nYour new password is {password}");
        }

        public async Task<string> ChangePassword(string email)
        {
            var res = await _userManager.FindByEmailAsync(email);
            var token = await _userManager.GeneratePasswordResetTokenAsync(res);

            var password = $"ace{DateTime.Now.Hour}{DateTime.Now.Minute}";

            await _userManager.ResetPasswordAsync(res, token, password);
            await _messagingService.SendEmailAsync(email, "Password Reset", $"Your password has been reset.\r\n\r\nYour new password is {password}");

            return password;
        }

        public async Task<bool> CheckPassword(AppUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<AppRole?> GetRoleFromRoleName(string name)
        {
            return await _roleManager.FindByNameAsync(name);
        }

        public async Task<string> GetRoleId(AppRole role)
        {
            return await _roleManager.GetRoleIdAsync(role);
        }

        public async Task<IList<string>> GetUserRoles(AppUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task RemoveUserFromRole(int userId, string role)
        {
            var userRole = await _dB.UserRoles.Where(o => o.UserId == userId).FirstOrDefaultAsync();
            _dB.UserRoles.Remove(userRole);
            await _dB.SaveChangesAsync();
        }

        public async Task UpdateUser(AppUser user, string fullname, string reg)
        {
            user.FullName = fullname;

            var profile = _dB.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);

            if (profile != null)
            {
                profile.RegNo = reg;
                _dB.UserProfiles.Update(profile);
            }

            await _dB.SaveChangesAsync();
            await _userManager.UpdateAsync(user);
        }

        public async Task UpdateFCMToken(AppUser user, string fcm_token)
        {
            var profile = _dB.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);

            if (profile != null)
            {
                profile.NotificationFCM = fcm_token;
                _dB.UserProfiles.Update(profile);
            }

            var existing = await _dB.Set<UserDeviceRegistration>()
                .FirstOrDefaultAsync(x => x.UserId == user.Id &&
                    x.DeviceType == UserDeviceType.MobileFcm &&
                    x.Token == fcm_token);

            if (existing == null)
            {
                await _dB.Set<UserDeviceRegistration>().AddAsync(new UserDeviceRegistration
                {
                    UserId = user.Id,
                    DeviceType = UserDeviceType.MobileFcm,
                    Token = fcm_token,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.IsActive = true;
                existing.UpdatedUtc = DateTime.UtcNow;
            }

            await _dB.SaveChangesAsync();

            _logger.LogInformation("FCM update succeeded for user {UserFullName}", user.FullName);
        }

        public async Task<string?> GetFCMToken(int userId)
        {
            var deviceToken = await _dB.Set<UserDeviceRegistration>()
                .AsNoTracking()
                .Where(o => o.UserId == userId && o.DeviceType == UserDeviceType.MobileFcm && o.IsActive)
                .OrderByDescending(o => o.UpdatedUtc ?? o.CreatedUtc)
                .Select(o => o.Token)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(deviceToken))
            {
                return deviceToken;
            }

            return await _dB.UserProfiles.AsNoTracking()
                .Where(o => o.UserId == userId)
                .Select(o => o.NotificationFCM)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetPhoneNumber(int userId)
        {
            return await _dB.Users.AsNoTracking().Where(o => o.Id == userId).Select(o => o.PhoneNumber).FirstOrDefaultAsync();
        }

        public async Task<bool> IsNonAce(int userId)
        {
            return await _dB.UserProfiles.AsNoTracking().Where(o => o.UserId == userId).Select(o => o.NonAce).FirstOrDefaultAsync();
        }

        public async Task<SendMessageOfType> GetCommsPlatform(int userId)
        {
            return await _dB.UserProfiles.AsNoTracking().Where(o => o.UserId == userId).Select(o => o.CommsPlatform).FirstOrDefaultAsync();
        }

        public async Task<string> GetFullname(int userId)
        {
            return await _dB.Users.AsNoTracking().Where(o => o.Id == userId).Select(o => o.FullName).FirstOrDefaultAsync();
        }

        public async Task<UsersListResponseDto> ListUsersAll()
        {
            var res = new UsersListResponseDto();

            try
            {
                var profiles = await _dB.UserProfiles
                    .Where(o => o.IsDeleted == false)
                    .AsNoTracking()
                    .Include(o => o.User)
                    .ToListAsync();

                profiles = profiles.OrderBy(o => o.User.LockoutEnabled).ToList();

                foreach (var profile in profiles)
                {
                    //if (profile.User.LockoutEnabled)
                    //    continue;

                    var roles = await _userManager.GetRolesAsync(profile.User);
                    var role = roles.FirstOrDefault();

                    AceRoles userRole = AceRoles.Driver;

                    switch (role)
                    {
                        case "Admin":
                            userRole = AceRoles.Admin;
                            break;
                        case "User":
                            userRole = AceRoles.User;
                            break;
                        case "Driver":
                            userRole = AceRoles.Driver;
                            break;
                    }

                    var obj = new ListedUser
                    {
                        Id = profile.User.Id,
                        FullName = profile.User.FullName,
                        Email = profile.User.Email,
                        RegNo = profile.RegNo,
                        PhoneNumber = profile.User.PhoneNumber,
                        ColorRGB = profile.ColorCodeRGB,
                        Role = userRole,
                        RoleString = role == "Admin" ? AceRoles.Admin.ToString() : AceRoles.Driver.ToString(),
                        IsLockedOut = profile.User.LockoutEnabled,
                        ShowAllBookings = profile.ShowAllBookings,
                        ShowHVSBookings = profile.ShowHVSBookings,
                        NonAce = profile.NonAce,
                        LastLogin = profile.LastLogin,
                        VehicleMake = profile.VehicleMake,
                        VehicleModel = profile.VehicleModel,
                        VehicleColour = profile.VehicleColour,
                        VehicleType = profile.VehicleType,
                        Comms = profile.CommsPlatform,
                        CashCommisionRate = profile.CashCommissionRate,
                        LockoutEnabled = profile.User.LockoutEnabled
                    };

                    res.Users.Add(obj);
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return res;

        }

        public async Task<UsersListResponseDto> ListUsers()
        {
            var res = new UsersListResponseDto();

            try
            {
                var profiles = await _dB.UserProfiles
                    .Where(o=>o.IsDeleted == false)
                    .AsNoTracking()
                    .Include(o => o.User)
                    .Where(o => o.User.LockoutEnabled == false) // exclude admin
                    .ToListAsync();

                foreach (var profile in profiles)
                {
                    //if (profile.User.LockoutEnabled)
                    //    continue;

                    var roles = await _userManager.GetRolesAsync(profile.User);
                    var role = roles.FirstOrDefault();

                    AceRoles userRole = AceRoles.Driver;

                    switch (role)
                    {
                        case "Admin":
                            userRole = AceRoles.Admin;
                            break;
                        case "User":
                            userRole = AceRoles.User;
                            break;
                        case "Driver":
                            userRole = AceRoles.Driver;
                            break;
                    }

                    var obj = new ListedUser
                    {
                        Id = profile.User.Id,
                        FullName = profile.User.FullName,
                        Email = profile.User.Email,
                        RegNo = profile.RegNo,
                        PhoneNumber = profile.User.PhoneNumber,
                        ColorRGB = profile.ColorCodeRGB,
                        Role = userRole,
                        RoleString = role == "Admin" ? AceRoles.Admin.ToString() : AceRoles.Driver.ToString(),
                        IsLockedOut = profile.User.LockoutEnabled,
                        ShowAllBookings = profile.ShowAllBookings,
                        ShowHVSBookings = profile.ShowHVSBookings,
                        NonAce = profile.NonAce,
                        LastLogin = profile.LastLogin,
                        VehicleMake = profile.VehicleMake,
                        VehicleModel = profile.VehicleModel,
                        VehicleColour = profile.VehicleColour,
                        VehicleType = profile.VehicleType,
                        Comms = profile.CommsPlatform, 
                        CashCommisionRate = profile.CashCommissionRate,
                        Username = profile.User.UserName
                    };

                    res.Users.Add(obj);
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return res;
        }

        public async Task<AppUser?> FindByName(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _userManager.FindByNameAsync(username);

                if (user != null)
                {
                    return user;
                }

                throw new NotFoundException($"username {username} was not found.");
            }

            throw new ArgumentException("username cannot be null");
        }

        public async Task<UserProfile?> GetProfile(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                var uid = await _dB.Users
                    .Where(o => o.UserName == username)
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync();

                var profile = await _dB.UserProfiles
                    .Include(o=>o.User)
                    .Where(o=>o.UserId == uid)
                    .FirstOrDefaultAsync();

                if (profile != null)
                {
                    return profile;
                }

                throw new NotFoundException($"username {username} was not found.");
            }

            throw new ArgumentException("username cannot be null");
        }

        public async Task UpdateProfile(UserProfile profile)
        {
            _dB.UserProfiles.Update(profile);
            await _dB.SaveChangesAsync();
        }

        public async Task<AppUser?> FindById(int id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(o => o.Id == id);
            if (user != null)
            {
                return user;
            }
            return null;
        }

        public async Task<string?> GetUsername(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if(user == null)
                throw new NotFoundException($"user id {id} was not found.");

            return user.UserName;
        }

        public async Task<bool> IsLockedOut(AppUser user)
        { 
            return _dB.Users.Where(o=>o.Id == user.Id).AsNoTracking().Select(o=>o.LockoutEnabled).FirstOrDefault();
        }

        public async Task LockoutOnOff(int userId, bool lockout)
        {
            var user = await _dB.Users.Where(o => userId == o.Id).FirstOrDefaultAsync();
            user.LockoutEnabled = lockout;
            user.LockoutEnd = DateTime.UtcNow.AddYears(5);
            _dB.Users.Update(user);
            await _dB.SaveChangesAsync();
        }

        public async Task ShowAllOnOff(int userId, bool show)
        {
            await _dB.UserProfiles.Where(o=>o.UserId == userId).ExecuteUpdateAsync(o => o.SetProperty(u => u.ShowAllBookings, show));
        }

        public async Task ShowHVSOnOff(int userId, bool show)
        {
            await _dB.UserProfiles.Where(o => o.UserId == userId).ExecuteUpdateAsync(o => o.SetProperty(u => u.ShowHVSBookings, show));
        }

        public async Task SetProfileDeleted(int userId, bool deleted)
        {
            var profile = await _dB.UserProfiles.Where(o => userId == o.UserId).FirstOrDefaultAsync();
            profile.IsDeleted = deleted;

            var bookings = await _dB.Bookings
                .Where(o => userId == o.UserId &&
                    o.PickupDateTime >=  DateTime.UtcNow)
                .ToListAsync();

            bookings.ForEach(booking => booking.UserId = null);

            _dB.Bookings.UpdateRange(bookings);
            _dB.UserProfiles.Update(profile);

            await _dB.SaveChangesAsync();
        }

        private static readonly ConcurrentDictionary<int, DateTime> _lastCacheWrites = new();
        public async Task UpdateGpsPosition(AppUser user, decimal longitude, decimal latitude, decimal speed, decimal heading)
        { 
            var now = DateTime.Now.ToUKTime();

            if (_lastCacheWrites.TryGetValue(user.Id, out var last) &&
                now - last < TimeSpan.FromSeconds(5))
            {
                _logger.LogDebug($"GPS Cache Update Skipped for {user.Id} -  {longitude}, {latitude}");
                return; 
            }

            _lastCacheWrites[user.Id] = now;

            try
            {
                await _cacheLocation.SetAsync(new CachedLocation
                {
                    UserId = user.Id,
                    Latitude = latitude,
                    Longitude = longitude,
                    Speed = speed,
                    Heading = heading,
                    Timestamp = now
                });

                _logger.LogDebug($"GPS Cached {user.Id} -  {longitude}, {latitude}");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable – skipping GPS cache update");
            }
            

           var profile = _dB.UserProfiles.FirstOrDefault(o => o.UserId == user.Id);

            if (profile != null)
            {
                profile.Latitude = latitude;
                profile.Longitude = longitude;
                profile.Speed = speed;
                profile.Heading = heading;
                profile.GpsLastUpdated = now;

                _dB.UserProfiles.Update(profile);
                // _logger.LogInformation("Profile updated");
            }
            else
            {
                var profil = new UserProfile();
                profil.UserId = user.Id;
                profil.Latitude = latitude;
                profil.Longitude = longitude;
                profil.Speed = speed;
                profil.Heading = heading;
                profil.GpsLastUpdated = now;

                await _dB.UserProfiles.AddAsync(profil);
            }

            // var entry = new DriverLocationHistory();
            // entry.UserId = user.Id;
            // entry.Latitude = latitude;
            // entry.Longitude = longitude;
            // entry.Speed = speed;
            // entry.Heading = heading;
            // entry.TimeStamp = DateTime.Now.ToUKTime();

            //// await _dB.DriverLocationHistories.AddAsync(entry);

            try
            {
                await _dB.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving GPS position for user {user.UserName} ({user.Id}) - {latitude}, {longitude} - {speed}mph - {heading} degrees", ex);
                _logger.LogError(ex.StackTrace);
            }
        }

        public async Task<UserGPSResponseDto> GetGpsPosition(int userid)
        {
            // Get Current GPS for specific user 
            var profile = await _dB.UserProfiles
                .AsNoTracking()
                .Include(o=>o.User)
                .Where(o => o.UserId == userid)
                .FirstOrDefaultAsync();

            if (profile != null)
            {
                if (profile.Longitude.HasValue && 
                    profile.Latitude.HasValue && 
                    profile.GpsLastUpdated.HasValue)
                {
                    if (profile.GpsLastUpdated.Value.Date == DateTime.Now.Date)
                    {
                        return new UserGPSResponseDto()
                        {
                            UserId = userid,
                            Username = profile.User.UserName,
                            Fullname = profile.User.FullName,
                            RegNo = profile.RegNo,
                            HtmlColor = GetHtmlColor(profile.ColorCodeRGB),
                            Latitude = Convert.ToDouble(profile.Latitude.Value),
                            Longitude = Convert.ToDouble(profile.Longitude.Value),
                            Heading = profile.Heading.Value,
                            Speed = Convert.ToDouble(profile.Speed.Value),
                            GpsLastUpdated = profile.GpsLastUpdated.Value
                        };
                    }
                }
                return null;
            }
            else 
                return null;
        }

        public async Task<List<UserGPSResponseDto>> GetAllCurrentGpsPositionsCache()
        {
            var list = new List<UserGPSResponseDto>();

            var locations = await _cacheLocation.GetAllAsync();

            _logger.LogDebug($"GPS Positions Retrieved from Cache: {locations.Count}");

            foreach (var location in locations) 
            {
                var profile = await _cacheProfile.GetAsync(location.UserId);
                
                if (profile != null)
                {
                    var obj = new UserGPSResponseDto()
                    {
                        UserId = profile.UserId,
                        Username = profile.Username,
                        Fullname = profile.FullName,
                        RegNo = profile.Registation,
                        HtmlColor = GetHtmlColor(profile.HtmlColorCode),
                        Latitude = Convert.ToDouble(location.Latitude),
                        Longitude = Convert.ToDouble(location.Longitude),
                        Speed = Convert.ToDouble(location.Speed),
                        Heading = location.Heading,
                        GpsLastUpdated = location.Timestamp
                    };
                    
                    list.Add(obj);
                }
            }

            return list;
        }

        /// <summary>
        /// Maybe Obsolete - Get GPS positions from the database
        /// </summary>
        /// <returns></returns>
        public async Task<List<UserGPSResponseDto>> GetAllCurrentGpsPositionsDB()
        {
            // Get current GPS for all users 
            var list = new List<UserGPSResponseDto>();

            var users = await _userManager.Users.ToListAsync();
            var profiles = await _dB.UserProfiles.AsNoTracking().Include(o=>o.User).ToListAsync();

            foreach(var profile in profiles) 
            {
                if (profile.Longitude.HasValue && profile.Latitude.HasValue && profile.GpsLastUpdated.HasValue)
                {
                    if(profile.GpsLastUpdated.Value.Date >= DateTime.Now.Date)
                    {
                        var obj = new UserGPSResponseDto()
                        {
                            UserId = profile.UserId,
                            Username = profile.User.UserName,
                            Fullname = profile.User.FullName,
                            RegNo = profile.RegNo,
                            HtmlColor = GetHtmlColor(profile.ColorCodeRGB),
                            Latitude = Convert.ToDouble(profile.Latitude.Value),
                            Longitude = Convert.ToDouble(profile.Longitude.Value),
                            GpsLastUpdated = profile.GpsLastUpdated.Value
                        };
                        
                        if(profile.Speed.HasValue)
                            obj.Speed = Convert.ToDouble(profile.Speed.Value);
                        if(profile.Heading.HasValue)
                            obj.Heading = profile.Heading.Value;

                        list.Add(obj);
                    }
                }
            }

            return list;
        }

        public async Task CreateUpdateAvailability(DriverAvailabilityDto dto)
        {
            var obj = await _dB.DriverAvailabilities
                .Where(o => o.UserId == dto.UserId && o.Date.Date == dto.Date.Date).FirstOrDefaultAsync();

            if (obj != null)
            {
                var oo = new BookingChangeAudit();

                oo.Action = "Modified";
                oo.EntityName = "DriverAvailabilities";
                oo.UserFullName = dto.FullName;
                oo.EntityIdentifier = obj.Id.ToString();
                oo.OldValue = obj.Description;
                oo.NewValue = dto.Description;
                oo.PropertyName = "Description";

                await _dB.BookingChangeAudits.AddAsync(oo);

                obj.Description = dto.Description;
                _dB.DriverAvailabilities.Update(obj);
            }
            else
            {
                obj = new DriverAvailability { Date = dto.Date, Description = dto.Description, UserId = dto.UserId };
                _dB.DriverAvailabilities.Add(obj);
            }

            await _dB.SaveChangesAsync();
        }

        public async Task CreateUpdateAvailability(AppUser user, DateTime date, string desc)
        {
            var obj = await _dB.DriverAvailabilities
                .Where(o=>o.UserId == user.Id &&
                o.Date.Date == date).FirstOrDefaultAsync();

            if (obj != null)
            {
                obj.Description = desc;
                _dB.DriverAvailabilities.Update(obj);
            }
            else
            {
                obj = new DriverAvailability { Date = date.Date, Description = desc, UserId = user.Id };
                _dB.DriverAvailabilities.Add(obj);
            }
            
            await _dB.SaveChangesAsync();
        }

        public async Task RemoveAvailability(DriverAvailabilityDto dto)
        {
            var obj = await _dB.DriverAvailabilities
                .Where(o => o.UserId == dto.UserId && o.Date.Date == dto.Date.Date).FirstOrDefaultAsync();
            if (obj != null)
            {
                _dB.DriverAvailabilities.Remove(obj);
                await _dB.SaveChangesAsync();
            }
        }

        public async Task<DriverAvailabilitiesDto> GetAvailabilities(DateTime date)
        {
            try
            {
                var data = await _dB.DriverAvailabilities
                .Where(o => o.Date.Date == date.Date.Date)
                .AsNoTracking()
                .OrderBy(o => o.Description)
                .ToListAsync();

                var lst = await _dB.UserProfiles.AsNoTracking().Include(o => o.User).Select(o => new
                {
                    UserId = o.UserId,
                    Color = o.ColorCodeRGB,
                    Fullname = o.User.FullName + " - (" + o.VehicleType.ToString() + ")"
                })
                    .AsNoTracking()
                    .ToListAsync();

                var res = new DriverAvailabilitiesDto();

                // -----------------------------------------------
                var dataGrp = data.GroupBy(o => o.UserId);

                foreach (var driver in dataGrp)
                {
                    var obj = lst.Where(o => o.UserId == driver.Key).FirstOrDefault();

                    var entrys = driver.ToList().OrderBy(o=>o.FromTime).ToList();
                    var str = string.Empty;
                    if (entrys.Count == 1)
                    {
                        if (entrys[0].FromTime == new TimeSpan(0, 0, 0) && entrys[0].ToTime == new TimeSpan(0, 0, 0))
                        {
                            str = entrys[0].Description;
                        }
                        else
                        {
                            var got = entrys[0].GiveOrTake ? "(+/-)" : "";
                            var des = string.IsNullOrEmpty(entrys[0].Description) ? "" : $"[{entrys[0].Description}]";

                            if (entrys[0].AvailabilityType == DriverAvailabilityType.Available)
                            {
                                if(des == "[AM SR]" || des == "[PM SR]")
                                    if(des == "[AM SR]")
                                        str = $"AM SCHOOL RUN - {got}";
                                    else
                                        str = $"PM SCHOOL RUN - {got}";
                                else
                                    str = $"{entrys[0].FromTime.ToString("hh\\:mm")} - {entrys[0].ToTime:hh\\:mm} {got} {des}";
                            }
                            else
                            {
                                str = "UNAVAILABLE";
                            }
                        }
                    }
                    else
                    {
                        foreach (var entry in entrys)
                        {
                            var got = entry.GiveOrTake ? "(+/-)" : "";
                            var des = string.IsNullOrEmpty(entry.Description) ? "" : $"[{entry.Description}]";

                            if (entry.AvailabilityType == DriverAvailabilityType.Available)
                            {
                                if (des == "[AM SR]" || des == "[PM SR]")
                                {
                                    if (string.IsNullOrEmpty(str))
                                    {
                                        if (des == "[AM SR]")
                                            str = $"AM SCHOOL RUN - {got}";
                                        else
                                            str = $"PM SCHOOL RUN - {got}";
                                    }
                                    else
                                    {
                                        if (des == "[AM SR]")
                                            str += $" | AM SCHOOL RUN - {got}";
                                        else
                                            str += $" | PM SCHOOL RUN - {got}";
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(str))
                                        str = $"{entry.FromTime:hh\\:mm} - {entry.ToTime:hh\\:mm} {got} {des}";
                                    else
                                        str += $" | {entry.FromTime:hh\\:mm} - {entry.ToTime:hh\\:mm} {got} {des}";
                                }
                            }
                            else
                            {
                                str = "UNAVAILABLE";
                            }
                        }
                    }

                    var obj1 = new DriverAvailabilityDto
                    {
                        UserId = driver.Key,
                        Date = date,
                        Description = str,
                        FullName = obj.Fullname, 
                    };

                    obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                    res.Drivers.Add(obj1);

                }

                res.Drivers = res.Drivers.OrderBy(o=>o.Description).ToList();

                // -----------------------------------------------

                //foreach (var item in data)
                //{
                //    var obj = lst.Where(o => o.UserId == item.UserId).FirstOrDefault();

                //    var obj1 = new DriverAvailabilityDto
                //    {
                //        UserId = item.UserId,
                //        Date = item.Date,
                //        Description = item.Description,
                //        FullName = obj.Fullname
                //    };

                //    obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                //    res.Drivers.Add(obj1);
                //}

                return res;
            }
            catch (Exception)
            {
                // expected on refresh
                return new DriverAvailabilitiesDto { };
            }
            
        }

        public async Task<DriverAvailabilitiesDto> GetAvailabilities(int userid)
        {
            var data = await _dB.DriverAvailabilities
                .Where(o => o.UserId == userid && o.Date > DateTime.Now.Date.AddDays(-1))
                .AsNoTracking()
                .OrderBy(o => o.Description)
                .ToListAsync();

            var res = new DriverAvailabilitiesDto();

            var lst = await _dB.UserProfiles.Where(o => o.UserId == userid)
                .AsNoTracking().Include(o => o.User).Select(o => new {
                UserId = o.UserId,
                Color = o.ColorCodeRGB,
                Fullname = o.User.FullName
            })
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in data)
            {
                var obj = lst.Where(o => o.UserId == item.UserId).FirstOrDefault();

                var obj1 = new DriverAvailabilityDto
                {
                    UserId = item.UserId,
                    Date = item.Date,
                    Description = item.Description,
                    FullName = obj.Fullname
                };

                obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                res.Drivers.Add(obj1);
            }

            return res;
        }

        #region Driver Expenses
        public async Task<List<DriverExpenseDto>> GetDriverExpenses(DateTime from, DateTime to,bool all, int userid = 0)
        {
            if (all)
            {
                to = to.To2359();
                var data = await _dB.DriverExpenses.Where(o=>o.Date.Date >= from.Date &&
                    o.Date <= to).ToListAsync();

                var res = _mapper.Map<List<DriverExpenseDto>>(data);

                return res;
            }
            else
            {
                to = to.To2359();
                var data = await _dB.DriverExpenses.Where(o => o.UserId == userid && (o.Date.Date >= from.Date &&
                    o.Date <= to)).ToListAsync();

                var res = _mapper.Map<List<DriverExpenseDto>>(data);

                return res;
            }
        }

        public async Task AddDriverExpense(DriverExpenseDto obj)
        {
            var res = _mapper.Map<DriverExpense>(obj);

            var date = DateTime.Now.ToUKTime();
            res.Date = date;

            await _dB.DriverExpenses.AddAsync(res);
            await _dB.SaveChangesAsync();
        }

        #endregion

        public async Task<int> ImportCsv(string filepath)
        {
            var cnt = 0;
            var lines = await File.ReadAllLinesAsync(filepath);

            var users = new List<UserProfile>();

            for (int i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(",");

                var fname = fields[0].Trim();
                var reg = fields[1].Trim();
                var email = fields[2].Trim();
                var phone = fields[3].Trim();
                var username = fields[4].Trim();

                var result = await Create(new AppUser()
                {
                    UserName = username,
                    Email = email,
                    PhoneNumber = phone,
                    FullName = fname,
                }, CreatePassword(6),
                new UserProfile
                {
                    ColorCodeRGB = "#938E8C",
                    RegNo = reg,
                    ShowAllBookings = false
                });

                if (result.Succeeded)
                    cnt++;
            }

            return cnt;
        }


        #region Private
        private string GetHtmlColor(string ColorRGB)
        {
            if (string.IsNullOrEmpty(ColorRGB))
            {
                return "#000000";
            }
            else if (ColorRGB.StartsWith("#"))
            {
                return ColorRGB;
            }

            var arr = ColorRGB.Split(',');

            int r = Convert.ToInt32(arr[0]);
            int g = Convert.ToInt32(arr[1]);
            int b = Convert.ToInt32(arr[2]);

            var s = string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);

            return s;
        }

        public string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        public async Task<int> GetUserFromPhoneNo(string telephone)
        {
            var id = await _dB.Users
                .Where(o=>o.PhoneNumber == telephone)
                .AsNoTracking()
                .Select(o=>o.Id)
                .FirstOrDefaultAsync();

            return id;
        }
        #endregion
    }
}




