#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.LocalPOI;
using TaxiDispatch.DTOs.User;
using TaxiDispatch.DTOs.User.Requests;
using TaxiDispatch.Features.AccountUsers.CreateUser;
using TaxiDispatch.Features.DriverUsers.CreateUser;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiAuthorize]
    public class AdminUIController : ControllerBase
    {
        private readonly AvailabilityService _availabilityService;
        private readonly UserManager<AppUser> _userManager;
        private readonly AdminUIService _adminUIService;
        private readonly TariffService _tariffService;
        private readonly AceMessagingService _messagingService;
        private readonly LocalPOIService _poiService;
        private readonly AccountsService _accountsService;
        private readonly UserProfileService _profileService;
        private readonly BookingService _bookingService;
        private readonly CreateDriverUserService _createDriverUserService;
        private readonly CreateAccountUserService _createAccountUserService;
        

        public AdminUIController(
            AvailabilityService availabilityService,
            AdminUIService adminUIService,
            UserManager<AppUser> userManager,
            TariffService tariffService,
            AceMessagingService messagingService, 
            LocalPOIService poiService,
            AccountsService accountsService,
            UserProfileService profileService,
            BookingService bookingService,
            CreateDriverUserService createDriverUserService,
            CreateAccountUserService createAccountUserService
            )
        {
            _availabilityService = availabilityService;
            _adminUIService = adminUIService;
            _userManager = userManager;
            _tariffService = tariffService; 
            _messagingService = messagingService;
            _poiService = poiService;
            _accountsService = accountsService;
            _profileService = profileService;
            _bookingService = bookingService;
            _createDriverUserService = createDriverUserService;
            _createAccountUserService = createAccountUserService;
        }

        private int? GetCurrentUserId()
        {
            var raw = User.FindFirst("id")?.Value;
            return int.TryParse(raw, out var userId) ? userId : null;
        }


        [HttpGet]
        [Route("Move9014To10026")]
        public async Task<IActionResult> Move9014To10026(DateTime from, DateTime to, bool action)
        {
            return Ok(await _adminUIService.Move9014To10026Async(from, to, action));
        }

        #region SEND MESSAGE
        [HttpPost]
        [Route("SendMessageToDriver")]
        [ApiAuthorize]
        public async Task<IActionResult> SendMessageToDriver(int driver, string message)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                await _adminUIService.SendDriverMessage(driver, message, user.FullName);
                return Ok();
            }

            return NotFound("the user (sender) was not found.");

        }

        [HttpPost]
        [Route("SendMessageToAllDrivers")]
        [ApiAuthorize]
        public async Task<IActionResult> SendMessageToAllDrivers(string message)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                await _adminUIService.SendAllDriversMessage(message, user.FullName);
                return Ok();
            }

            return NotFound("the user (sender) was not found.");
        }

        

        #endregion

        [ApiAuthorize]
        [HttpGet]
        [Route("Dashboard")]
        public async Task<IActionResult> GetDashData()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null) 
                {
                    return Ok(await _adminUIService.GetDashData(user.Id));
                }

                return NotFound("the user was not found.");
            }

            return NotFound("the username was not returned from identity.");
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetSMSHeartBeat")]
        public async Task<IActionResult> GetHeartbeat()
        {
            return Ok(await _adminUIService.GetSMSHeartBeat());
        }

        //[ApiAuthorize]
        [HttpGet]
        [Route("DriversOnShift")]
        public async Task<IActionResult> DriversOnShift()
        {
            var data = await _availabilityService.GetOnShiftDrivers();
            return Ok(data);
        }

        [HttpPost]
        [Route("DriverEarningsReport")]
        public async Task<IActionResult> DriverEarningsReport(DriverEarningsRequestDto dto)
        {
            var res = new DriverExpensesResponseDto();
            var data = await _accountsService.GetEarningsWithinRange(dto.From, dto.To, dto.UserId);

            var obj = new JobsCountModelDto();

            foreach (var item in data)
            {
                obj.AccJobsCount += item.AccJobsCount;
                obj.CashJobsCount += item.CashJobsCount;
                obj.RankJobsCount += item.RankJobsCount;
            }

            res.JobsCount = obj;

            res.JobCountDateRangeLabels = new string[] { $"Cash Jobs ({obj.CashJobsCount})", $"Account Jobs ({obj.AccJobsCount})", $"Rank Jobs ({obj.RankJobsCount})" };
            res.JobCountDateRangeValues = new double[] { obj.CashJobsCount, obj.AccJobsCount, obj.RankJobsCount };
            res.Earnings = data;

            return Ok(res);
        }

        #region DRIVER EXPENSES
        [HttpPost]
        [Route("DriverExpenses")]
        public async Task<IActionResult> DriverExpenses(GetDriverExpensesRequestDto req)
        {
            if (!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            return Ok(await _adminUIService.GetDriverExpensesAsync(req));
        }
        #endregion

        #region DRIVERS
        [HttpGet]
        [Route("DriversList")]
        public async Task<IActionResult> DriversList()
        {
            var res = await _profileService.ListUsersAll();
            var users = res.Users.ToList().OrderBy(o => o.LockoutEnabled).ToList();

            return Ok(users);
        }

        [HttpPost]
        [Route("DriverAdd")]
        public async Task<IActionResult> DriverAdd(UserRegistrationRequestDto obj)
        {
            var res = await _createDriverUserService.CreateAsync(new CreateDriverUserRequest
            {
                Username = obj.Username,
                Fullname = obj.Fullname,
                RegistrationNo = obj.RegistrationNo,
                PhoneNumber = obj.PhoneNumber,
                Email = obj.Email,
                Password = obj.Password,
                ColorCode = obj.ColorCode,
                ShowAllBookings = obj.ShowAllBookings,
                ShowHvsBookings = false,
                VehicleColor = obj.VehicleColor,
                VehicleMake = obj.VehicleMake,
                VehicleModel = obj.VehicleModel,
                VehicleType = obj.VehicleType,
                CashCommissionRate = 15,
                Comms = obj.Comms
            }, GetCurrentUserId());

            if (res.Success)
                return Ok();
            else
            {
                return BadRequest($"Failed to create account user.\r\n{res.ErrorMessage}");
            }
        }

        [HttpPost]
        [Route("DriverUpdate")]
        public async Task<IActionResult> DriverUpdate(UserUpdateRequestDto obj)
        {
            if (obj.UserId == 0)
            {
                return BadRequest("Invalid user id");
            }

            var username = await _profileService.GetUsername(obj.UserId);
            var profile = await _profileService.GetProfile(username);

            if (profile != null)
            {
                profile.User.FullName = obj.Fullname;
                profile.User.Email = obj.Email;
                profile.User.PhoneNumber = obj.PhoneNumber;
                profile.RegNo = obj.RegistrationNo;
                profile.ColorCodeRGB = obj.ColorCode;
                profile.ShowAllBookings = obj.ShowAllBookings;
                profile.VehicleColour = obj.VehicleColor;
                profile.VehicleMake = obj.VehicleMake;
                profile.VehicleModel = obj.VehicleModel;
                profile.VehicleType = obj.VehicleType;
                profile.ShowHVSBookings = obj.ShowAllBookings;
                profile.CashCommissionRate = obj.CashCommisionRate;
                profile.CommsPlatform = obj.Comms;
                profile.NonAce = obj.NonAce;

                // update roles
                var roles = await _userManager.GetRolesAsync(profile.User);

                foreach (var role in roles)
                {
                    await _profileService.RemoveUserFromRole(profile.UserId, role);
                }

                await _profileService.AddUserToRole(profile.User, obj.Role.ToString());
                await _profileService.UpdateProfile(profile);
            }

            return Ok();
        }

        [HttpPost]
        [Route("DriverDelete")]
        public async Task<IActionResult> DriverDelete(int userId)
        {
            if (userId == 0)
            {
                return BadRequest("Invalid user id");
            }
            
            await _profileService.SetProfileDeleted(userId, true);
            return Ok();
        }

        [HttpGet]
        [Route("DriverResendLogin")]
        public async Task<IActionResult> DriverResendLogin(int userId)
        {
            if (userId == 0)
            {
                return BadRequest("Invalid user id");
            }

            await _profileService.ChangePassword(userId);

            return Ok();
        }

        [HttpGet]
        [Route("DriverShowAllJobs")]
        public async Task<IActionResult> DriverShowAllJobs(int userId, bool turnOn)
        {
            if (userId == 0)
            {
                return BadRequest("Invalid user id");
            }

            await _profileService.ShowAllOnOff(userId, turnOn);
            return Ok();
        }

        [HttpGet]
        [Route("DriverShowHVSJobs")]
        public async Task<IActionResult> DriverShowHVSJobs(int userId, bool turnOn)
        {
            if (userId == 0)
            {
                return BadRequest("Invalid user id");
            }

            await _profileService.ShowHVSOnOff(userId, turnOn);
            return Ok();
        }

        [HttpGet]
        [Route("DriverLockout")]
        public async Task<IActionResult> DriverLockout(int userId, bool lockout)
        {
            if (userId == 0)
            {
                return BadRequest("Invalid user id");
            }
            
            await _profileService.LockoutOnOff(userId, lockout);
            return Ok();
        }

        [HttpGet]
        [Route("GetDriverExpirys")]
        public async Task<IActionResult> DriverExpirys()
        {
            return Ok(await _adminUIService.GetDriverExpirysAsync());
        }

        [HttpPost]
        [Route("UpdateDriverExpiry")]
        public async Task<IActionResult> UpdateDriverExpiry(UpdateExpiryDto dto)
        {
            if (dto.IsValid)
            {
                await _adminUIService.UpdateDriverExpiryAsync(dto);
                return Ok();
            }

            return BadRequest("Invalid data or missing required fields");

        }

        #endregion

        #region SEARCH
        [HttpPost]
        [Route("BookingsByStatus")]
        public async Task<IActionResult> DriverEarningsReport(DateTime date, BookingScope scope, BookingsByStatus status)
        {
            switch(status)
            {
                case BookingsByStatus.Cancelled:
                    {
                        var data = await _bookingService.GetCancelledJobs(date);

                        if (scope != BookingScope.All)
                        {
                            data = data.Where(o => o.Scope == scope).ToList();
                        }

                        return Ok(data.OrderByDescending(o => o.PickupDateTime).ToList());
                    }
                case BookingsByStatus.Completed:
                    {
                        var data = await _bookingService.GetCompletedJobs(date);

                        if (scope != BookingScope.All)
                        {
                            data = data.Where(o => o.Scope == scope).ToList();
                        }

                        return Ok(data.OrderByDescending(o => o.PickupDateTime).ToList());
                    }
                case BookingsByStatus.Allocated:
                    {
                        var data = await _bookingService.GetAllocatedJobs(date);

                        if (scope != BookingScope.All)
                        {
                            data = data.Where(o => o.Scope == scope).ToList();
                        }

                        return Ok(data.OrderByDescending(o => o.PickupDateTime).ToList());
                    }
                case BookingsByStatus.Unallocated:
                    {
                        var data = await _bookingService.GetUnallocatedJobs(date);

                        if (scope != BookingScope.All)
                        {
                            data = data.Where(o => o.Scope == scope).ToList();
                        }

                        return Ok(data.OrderByDescending(o => o.PickupDateTime).ToList());
                    }
                default:
                    return BadRequest("Invalid status");
            }
        }

        #endregion

        #region BOOKINGS

        [HttpGet]
        [Route("AirportRuns")]
        public async Task<IActionResult> AirportRuns(int months)
        {
            var lastJourneys = new List<AirportSearchModel.LastTripModel>();
            months = months * -1;

            var date = DateTime.Now.AddMonths(months);
            var data = await _bookingService.GetAirportRuns(date);

            lastJourneys.Clear();

            var grp = data.LastAirports.GroupBy(o => o.UserId).ToList();

            foreach (var u in grp)
            {
                var max = u.Max(o => o.Date);
                var obj = u.FirstOrDefault(o => o.Date == max);

                lastJourneys.Add(obj);
            }

            lastJourneys = lastJourneys.OrderBy(o => o.UserId).ToList();
            var res = new { data, lastJourneys };

            return Ok(res);
        }

        [HttpGet]
        [Route("BookingAudit")]
        public async Task<IActionResult> BookingAudit(int bookingId)
        {
            if (bookingId == 0)
            {
                return BadRequest("Invalid booking id");
            }

            var data = await _bookingService.GetAuditLog(bookingId.ToString());

            return Ok(data);
        }

        [HttpGet]
        [Route("CardBookings")]
        public async Task<IActionResult> CardBookings()
        {
            var data = await _bookingService.GetActiveCardJobs();
            data = data.OrderByDescending(o => o.PickupDateTime).ToList();

            return Ok(data);
        }

        [HttpPost]
        [Route("SendCardPaymentReminder")]
        public async Task<IActionResult> SendCardPaymentReminder(SendCardPaymentReminderDto req)
        {
            if(req.IsValid)
                return BadRequest("Invalid data or missing required fields");

            var data = await _bookingService.GetPaymentLink(req.BookingId);

            if (!string.IsNullOrEmpty(data))
            { 
                _messagingService.SendPaymentLinkReminderSMS(req.Phone, data);
                return Ok();
            }
            else
            {
                return NotFound("Payment link not found");
            }
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("CancelBookingsInRange")]
        public async Task<IActionResult> CancelBookingsInRange(DeleteByRangeRequestDto req)
        {
            if(!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                    await _bookingService.CancelBookingsByDateRange(req,user.FullName);
                    return Ok();
            }

            return NotFound("the username was not found.");
        }
        [ApiAuthorize]
        [HttpPost]
        [Route("CancelBookingsInRangeReport")]
        public async Task<IActionResult> CancelBookingsInRangeReport(DeleteByRangeRequestDto req)
        {
            if (!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                var data = await _bookingService.CancelBookingsByDateRangeReport(req, user.FullName);
                return Ok(data);
            }

            return NotFound("the username was not found.");
        }

        [HttpPost]
        [Route("GetTurndowns")]
        public async Task<IActionResult> GetTurndowns(DateTime from, DateTime to)
        {
            return Ok(await _adminUIService.GetTurnDownsAsync(from, to));
        }

        #endregion

        #region ACCOUNTS
        [HttpPost]
        [Route("AddAccount")]
        public async Task<IActionResult> AddAccount(AccountDto req)
        {
            if (req.IsValid)
            {
                var accno = await _accountsService.CreateAccount(req);
                return Ok(accno);
            }
            return BadRequest("Invalid data or missing required fields");
        }

        [HttpPost]
        [Route("UpdateAccount")]
        public async Task<IActionResult> UpdateAccount(AccountDto req)
        {
            if (req.IsValid)
            {
                await _accountsService.UpdateAccount(req);
                return Ok();
            }
            return BadRequest("Invalid data or missing required fields");
        }

        [HttpGet]
        [Route("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount(int accno)
        {
            if (accno == 0)
            {
                return BadRequest("Invalid account number");
            }

            // TODO:: Add delete account logic
            await _accountsService.DeleteAccount(accno);

            return Ok();
        }

        [HttpGet]
        [Route("GetAccounts")]
        public async Task<IActionResult> GetAccounts()
        {
            var data = await _accountsService.GetAllAccounts();
            return Ok(data);
        }

        [HttpPost]
        [Route("RegisterAccountWebBooker")]
        public async Task<IActionResult> RegisterAccountWebBooker(RegisterAccountWebBookerDto req)
        {
            if (!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            //password 
            var password = GenerateRandomString();
           // var password = $"!AccountUser{req.Accno}!";

            var res = await _createAccountUserService.CreateAsync(new CreateAccountUserRequest
            {
                Username = req.Accno.ToString(),
                Fullname = req.BookerName,
                PhoneNumber = req.BookerPhone,
                Email = req.BookerEmail,
                Password = password,
                AccNo = req.Accno
            }, GetCurrentUserId());

            if (res.Success)
                return Ok();
            else
            { 
                return BadRequest($"Failed to create account user.\r\n{res.ErrorMessage}"); 
            }
        }

        #endregion

        #region AVAILABILITY

        [HttpGet]
        [Route("AvailabilityLog")]
        public async Task<IActionResult> AvailabilityLog(int userid, DateTime date)
        {
            if (date != null)
            {
                if (userid > 0)
                {
                    var data = await _availabilityService.GetAuditLog(date, userid);
                    return Ok(data);
                }
                else
                {
                    var data = await _availabilityService.GetAuditLog(date);
                    return Ok(data);
                }
            }

            return BadRequest("Invalid date");
        }

        [HttpGet]
        [Route("GetAvailability")]
        public async Task<IActionResult> GetAvailability(int userid, DateTime date)
        {

            if (userid == 0)
            {
                var ok = await _availabilityService.GetAvailabilities(date); 
                return Ok(ok);
            }
            else
            { 
                var ok = await _availabilityService.GetAvailabilities(userid,date);
                return Ok(ok);
            }

            
        }

        [HttpGet]
        [Route("DeleteAvailability")]
        [ApiAuthorize]
        public async Task<IActionResult> DeleteAvailability(int availabilityId)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);
            }

            await _availabilityService.Delete(availabilityId, uname);

            return Ok();
        }

        [HttpPost]
        [Route("SetAvailability")]
        [ApiAuthorize]
        public async Task<IActionResult> SetAvailability(CreateAvailabilityRequestDto req)
        {
            if (req.IsValid)
            {
                var uname = User.Identity.Name;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        var start = DateTime.Now.ToUKTime().AddMonths(-12);
                        var end = DateTime.Now.ToUKTime();


                        var fhr = Convert.ToInt32(req.From.Split(':')[0]);
                        var fmm = Convert.ToInt32(req.From.Split(':')[1]);

                        var thr = Convert.ToInt32(req.To.Split(':')[0]);
                        var tmm = Convert.ToInt32(req.To.Split(':')[1]);

                        var fromTs = new TimeSpan(fhr, fmm, 0);
                        var toTs = new TimeSpan(thr, tmm, 0);


                        var data = await _availabilityService.Create(req.UserId, req.Date, fromTs, toTs,
                            (bool)req.GiveOrTake, req.Type, req.Note, user.FullName);

                        return Ok(data);
                    }
                    return NotFound("user not found.");
                }

                return NotFound("the username was not found.");
            }

            return BadRequest("the model data was invalid or missing required fields.");
        }

        [HttpPost]
        [Route("AvailabilityReport")]
        [ApiAuthorize]
        public async Task<IActionResult> AvailabilityReport(GetAvailabilityReportDto req)
        {
            if(!req.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            if (req.UserId > 0)
            {
                var data = await _availabilityService.GetAvailabilityForPeriod(req.StartDate, req.EndDate, req.UserId);
                if (data != null)
                {
                    return Ok(data);
                }

                return NotFound("No data found");
            }
            else
            {
                var data = await _availabilityService.GetAvailabilityForPeriod(req.StartDate, req.EndDate);
                if (data != null)
                {
                    return Ok(data);
                }

                return NotFound("No data found");
            }
        }

        #endregion

        #region POIS
        [HttpPost]
        [Route("AddPOI")]
        public async Task<IActionResult> AddPOI(CreatePOIRequest req)
        {
            if (req.IsValid)
            {
                await _poiService.CreatePOI(req);
                return Ok();
            }
            return BadRequest("Invalid data or missing required fields");
        }

        [HttpPost]
        [Route("UpdatePOI")]
        public async Task<IActionResult> UpdatePOI(UpdatePOIRequest req)
        {
            if (req.IsValid)
            {
                await _poiService.UpdatePOI(req);
                return Ok();
            }
            return BadRequest("Invalid data or missing required fields");
        }

        [HttpGet]
        [Route("DeletePOI")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            if (id == 0)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            var res = await _poiService.DeletePOI(new DeletePOIRequest()
            {
                Id = id
            });

            return Ok(id);
        }

        [HttpGet]
        [Route("GetPOIs")]
        public async Task<IActionResult> GetPOIs()
        {
            var data = await _poiService.GetLocalPOI("");
            return Ok(data);
        }

        #endregion

        #region CONFIG
        [HttpGet]
        [Route("GetMessageConfig")]
        public async Task<IActionResult> GetMessageConfig()
        {
            return Ok(await _adminUIService.GetMessageConfigAsync());
        }

        [HttpPost]
        [Route("UpdateMessageConfig")]
        public async Task<IActionResult> UpdateMessageConfig(MessagingNotifyConfig req)
        {
            await _adminUIService.UpdateMessageConfigAsync(req);
            return Ok();
        }

        [HttpGet]
        [Route("GetCompanyConfig")]
        public async Task<IActionResult> GetCompanyConfig()
        {
            return Ok(await _adminUIService.GetCompanyConfigAsync());
        }

        [HttpPost]
        [Route("UpdateCompanyConfig")]
        public async Task<IActionResult> AddCompanyConfig(CompanyConfig req)
        {
            await _adminUIService.UpdateCompanyConfigAsync(req);
            return Ok();
        }
        #endregion

        #region TARIFFS
        [HttpGet]
        [Route("GetTariffConfig")]
        public async Task<IActionResult> GetTariffConfig()
        {
            var data = await _tariffService.GetAllTariffs();
            return Ok(data);
        }

        [HttpPost]
        [Route("SetTariffConfig")]
        public async Task<IActionResult> SetTariffConfig(Tariff obj)
        {
            await _tariffService.Update(obj);
            return Ok();
        }

        #endregion

        #region UI NOTIFICATIONS
        [HttpGet]
        [Route("GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var res = await _adminUIService.GetNotifications();

            return Ok(res);
        }

        [HttpGet]
        [Route("ClearNotification")]
        public async Task<IActionResult> ClearNotification(int id)
        {
            await _adminUIService.ClearNotification(id);

            return Ok();
        }

        [HttpGet]
        [Route("ClearAllNotifications")]
        public async Task<IActionResult> ClearAllNotification()
        {
            await _adminUIService.ClearAllNotifications();

            return Ok();
        }

        [HttpPost]
        [Route("ClearAllNotifications")]
        public async Task<IActionResult> ClearAllNotificationbyType(NotificationEvent type)
        {
            await _adminUIService.ClearAllNotificationByType(type);

            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("UpdateBrowserFCM")]
        public async Task<IActionResult> UpdateBrowserFCM(string fcm)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    await _adminUIService.UpdateBrowserFcmAsync(user.Id, fcm);
                }
                else
                    return NotFound("the user was not found.");
            }

            return Ok();
        }

        [HttpPost]
        [Route("GetBrowserFCMs")]
        public async Task<IActionResult> GetBrowserFCMs()
        {
            return Ok(await _adminUIService.GetBrowserFcmsAsync());
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("RemoveBrowserFCM")]
        public async Task<IActionResult> RemoveBrowserFCM()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    await _adminUIService.ClearBrowserFcmAsync(user.Id);
                }
                else
                    return NotFound("the user was not found.");
            }

            return Ok();
        }

        #endregion

        #region WEB BOOKING REQUESTS
        [HttpGet]
        [Route("GetWebChangeRequests")]
        public async Task<IActionResult> GetWebChangeRequests()
        {
            return Ok(await _adminUIService.GetWebChangeRequestsAsync());
        }

        [HttpGet]
        [Route("UpdateWebChangeRequest")]
        public async Task<IActionResult> UpdateWebChangeRequests(int reqId)
        {
            await _adminUIService.UpdateWebChangeRequestAsync(reqId);
            return Ok();
        }

        #endregion

        [HttpPost]
        [Route("SubmitTicket")]
        [Consumes("multipart/form-data")] // Explicitly tells the API it expects form data
        public async Task<IActionResult> SubmitTicket([FromBody] SubmitTicketRequest request)
        {
            // You now have access to request.Subject, request.Message, and request.Attachment

            if (request.Attachment != null && request.Attachment.Length > 0)
            {
                // Process the file, e.g., save it to storage or pass it to the service
                // You can access file details like FileName and Length
                var fileName = request.Attachment.FileName;
                // Example: read the file stream
                 using var stream = request.Attachment.OpenReadStream();
                await _messagingService.SendEmailRaiseTicket(request.Subject, request.Message, stream, fileName);
            }
            else
            {
                await _messagingService.SendEmailRaiseTicket(request.Subject, request.Message, null, null);
            }
            return Ok();
        }

        private static string GenerateRandomString(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}




