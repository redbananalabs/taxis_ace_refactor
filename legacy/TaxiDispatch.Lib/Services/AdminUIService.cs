using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Admin;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.User.Requests;
using EllipticCurve.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiDispatch.Modules.Messaging;
using TaxiDispatch.Modules.Messaging.Services;

namespace TaxiDispatch.Services
{
    public class AdminUIService : BaseService<AdminUIService>
    {
        private readonly AvailabilityService _availabilityService;
        private readonly ReportingService _reportingService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly UserProfileService _usersService;
        private readonly AceMessagingService _messageService;

        public AdminUIService(
            IDbContextFactory<TaxiDispatchContext> factory,
            AvailabilityService availabilityService, 
            UserProfileService usersService,
            IPushNotificationService pushNotificationService,
            AceMessagingService messageService,
            ReportingService reportingService, ILogger<AdminUIService> logger) : base(factory, logger)
        {
            _availabilityService = availabilityService;
            _reportingService = reportingService;
            _pushNotificationService = pushNotificationService;
            _usersService = usersService;
            _messageService = messageService;
        }

        public async Task SendAllDriversMessage(string message, string sentBy)
        {
            // get drivers on shift
            var drivers = await _availabilityService.GetOnShiftDrivers();

            var lst = new List<(SendMessageOfType comms, string userid, string fcm, string telephone)>();

            foreach (var driver in drivers)
            {
                var fcm = await _usersService.GetFCMToken(driver.UserId);
                var tel = await _usersService.GetPhoneNumber(driver.UserId);
                var del = await _usersService.GetCommsPlatform(driver.UserId);

                lst.Add((del,driver.UserId.ToString(), fcm, tel));
            }

            foreach (var entry in lst)
            {
                var uid = Convert.ToInt32(entry.userid);
                var online = await IsPushOnline(uid);

                if (entry.comms == SendMessageOfType.Push && !string.IsNullOrEmpty(entry.fcm))
                {
                    _logger.LogInformation($"UserId {entry.userid} has FCM token - .");

                    var date = DateTime.Now.ToUKTime();

                    var navObj = new Dictionary<string, string>
                            {
                               { "NavId", $"{(int)PushNotificationNavId.GlobalMessage}" },
                               { "message", message },
                               { "datetime", date.ToString("dd/MM/yyyy HH:mm") },
                               { "sentBy", sentBy }
                            };

                     var pushn = new PushNotificationRequest
                     {
                        notification = new NotificationMessageBody
                        {
                            title = "GLOBAL MESSAGE",
                            body = message
                        },
                        data = navObj,
                        registration_ids = new List<string> { entry.fcm }
                     };

                     // send
                     try
                     {
                         await _pushNotificationService.SendAndroidNotification(pushn);

                         _logger.LogInformation($"Push notification sent to userId {entry.userid} with FCM token {entry.fcm}.");
                     }
                     catch (Exception ex)
                     {
                         _logger.LogError(ex, $"Push notification sent to userId {entry.userid} with FCM token {entry.fcm} failed");
                     }
                }
                else // text or whatsapp
                {
                    _messageService.SendSmsMessage(entry.telephone, message);
                }
            }
        }

        public async Task SendDriverMessage(int userid, string message, string sentBy)
        {
            var fcm = "";
            var tel = "";
            var del = SendMessageOfType.WhatsApp;

            // get user fcm token
            if (userid != null)
            {
                fcm = await _usersService.GetFCMToken(userid);
                tel = await _usersService.GetPhoneNumber(userid);
                del = await _usersService.GetCommsPlatform(userid);
            }

            var online = await IsPushOnline(userid);

            if (del == SendMessageOfType.Push && !string.IsNullOrEmpty(fcm) && online.Alive)
            {
                _logger.LogInformation($"UserId {userid} has FCM token - .");

                var date = DateTime.Now.ToUKTime();

                var navObj = new Dictionary<string, string>
                        {
                            { "NavId", $"{(int)PushNotificationNavId.DirectMessage}" },
                            { "message", message },
                            { "datetime", date.ToString("dd/MM/yyyy HH:mm") },
                            { "sentBy", sentBy }
                         };

                 var pushn = new PushNotificationRequest
                 {
                    notification = new NotificationMessageBody
                    {
                        title = "DIRECT MESSAGE",
                        body = message
                    },
                    data = navObj,
                    registration_ids = new List<string> { fcm }
                 };

                // send
                try
                {
                    await _pushNotificationService.SendAndroidNotification(pushn);

                    _logger.LogInformation($"Push notification sent to userId {userid} with FCM token {fcm}.");
                }
                catch (Exception ex)
                {
                   _logger.LogError(ex, $"Push notification sent to userId {userid} with FCM token {fcm} failed");
                }
            }
            else
            {
                _messageService.SendSmsMessage(tel, message);
            }
        }

        private async Task<(bool Alive, int Minutes)> IsPushOnline(int userId)
        {
            var time = await _dB.UserProfiles
                .Where(o => o.UserId == userId)
                .Select(o => o.GpsLastUpdated)
                .FirstOrDefaultAsync();

            if (time == null)
                return (false, 99);

            // Calculate the time difference
            TimeSpan difference = DateTime.Now.ToUKTime() - time.Value;

            // If you want whole minutes only:
            int diff = (int)difference.TotalMinutes;
            var off = diff <= 3;

            return (off, diff);
        }

        public async Task<DashboardDataDto> GetDashData(int userId)
        {
            var obj = new DashboardDataDto();

            var cnts = await _reportingService.GetDashCounts();
            obj.BookingsTodayCount = cnts.BookingsCount;
            obj.DriversCount = cnts.DriversCount;
            obj.PoisCount = cnts.POIsCount;
            obj.UnallocatedTodayCount = cnts.UnallocatedTodayCount;

            var from = DateTime.Now.StartOfWeek(DayOfWeek.Monday).Date;
            var to = from.AddDays(6).To2359();

            var all = await _reportingService.GetDriversTotals(from, to, userId);

            // remove driver 1
            all.RemoveAll(x => x.Id == 1);
            obj.DriverWeeksEarnings = all;

            var data = await _reportingService.GetDriversTotals(DateTime.Today.Date, DateTime.Now.ToUKTime().To2359(), userId);
            // remove driver 1
            data.RemoveAll(x => x.Id == 1);
            obj.DriverDaysEarnings = data;

            obj.JobsBookedToday = await _reportingService.JobsBookedToday();
            obj.JobsBookedTodayCount = obj.JobsBookedToday.Sum(o => o.Total);

            var counts = await _reportingService.GetCustomerCounts();
            obj.CustomerAquireCounts = counts;

            obj.AllocationReplys = await _reportingService.GetAllocationReplys();

            obj.SmsHeartBeat = await _reportingService.GetSmsHeartBeat();

            return obj;
        }

        public async Task<DateTime?> GetSMSHeartBeat()
        {
            return await _reportingService.GetSmsHeartBeat();
        }

        public async Task<List<UINotification>> GetNotifications()
        { 
            var data = await _dB.UINotifications.Where(o=>o.Status == NotificationStatus.Default)
                .OrderBy(o=>o.DateTimeStamp)
                .ToListAsync();

            return data;
        }

        public async Task ClearNotification(int id)
        {
            await _dB.UINotifications.Where(o => o.Id == id)
                .ExecuteUpdateAsync(o => o.SetProperty(p => p.Status, NotificationStatus.Read));
        }

        public async Task ClearAllNotificationByType(NotificationEvent eventType)
        {
            await _dB.UINotifications.Where(o => o.Event == eventType)
                .ExecuteUpdateAsync(o => o.SetProperty(p => p.Status, NotificationStatus.Read));
        }

        public async Task ClearAllNotifications()
        {
            await _dB.UINotifications
                .ExecuteUpdateAsync(o => o.SetProperty(p => p.Status, NotificationStatus.Read));
        }

        public async Task<List<AccountMigrationBookingDto>> Move9014To10026Async(DateTime from, DateTime to, bool applyChanges)
        {
            var homes = await _dB.LocalPOIs
                .Where(o => o.Type == LocalPOIType.House)
                .Select(o => o.Postcode)
                .ToListAsync();

            var normalizedHomes = homes
                .Select(h => h.Trim().ToLower())
                .ToList();

            var data = await _dB.Bookings
                .Where(o => !o.Cancelled &&
                            o.PickupDateTime >= from &&
                            o.PickupDateTime <= to &&
                            o.AccountNumber == 9014 &&
                            o.PickupPostCode == "DT9 4DN" &&
                            !normalizedHomes.Contains(o.DestinationPostCode.Trim().ToLower()))
                .Select(o => new AccountMigrationBookingDto
                {
                    Id = o.Id,
                    PickupDateTime = o.PickupDateTime,
                    PickupAddress = o.PickupAddress,
                    PickupPostCode = o.PickupPostCode,
                    DestinationAddress = o.DestinationAddress,
                    DestinationPostCode = o.DestinationPostCode,
                    PassengerName = o.PassengerName
                })
                .ToListAsync();

            if (applyChanges && data.Count > 0)
            {
                var ids = data.Select(o => o.Id).ToList();

                await _dB.Bookings
                    .Where(o => ids.Contains(o.Id))
                    .ExecuteUpdateAsync(o => o.SetProperty(u => u.AccountNumber, 10026));
            }

            return data;
        }

        public async Task<DriverExpensesSummaryDto> GetDriverExpensesAsync(GetDriverExpensesRequestDto req)
        {
            var to = req.To.To2359();

            var query = _dB.DriverExpenses
                .Where(o => o.Date >= req.From.Date && o.Date <= to);

            if (req.UserId > 0)
            {
                query = query.Where(o => o.UserId == req.UserId);
            }

            var data = await query.ToListAsync();

            return new DriverExpensesSummaryDto
            {
                Data = data,
                Total = data.Sum(o => o.Amount)
            };
        }

        public async Task<List<DocumentExpiry>> GetDriverExpirysAsync()
        {
            var data = await _dB.DocumentExpirys
                .AsNoTracking()
                .ToListAsync();

            data.RemoveAll(o => o.UserId == 0);

            var userIds = data
                .Select(o => o.UserId)
                .Distinct()
                .ToList();

            if (userIds.Count == 0)
            {
                return data;
            }

            var profiles = await _dB.UserProfiles
                .Where(o => userIds.Contains(o.UserId))
                .Include(o => o.User)
                .AsNoTracking()
                .Select(o => new
                {
                    o.UserId,
                    o.ColorCodeRGB,
                    FullName = o.User.FullName
                })
                .ToDictionaryAsync(o => o.UserId);

            foreach (var item in data)
            {
                if (profiles.TryGetValue(item.UserId, out var profile))
                {
                    item.ColorCode = profile.ColorCodeRGB;
                    item.Fullname = profile.FullName;
                }
            }

            return data;
        }

        public async Task UpdateDriverExpiryAsync(UpdateExpiryDto dto)
        {
            var existing = await _dB.DocumentExpirys
                .FirstOrDefaultAsync(o => o.DocumentType == dto.DocType && o.UserId == dto.UserId);

            if (existing != null)
            {
                existing.ExpiryDate = dto.ExpiryDate;
                existing.LastUpdatedOn = DateTime.Now.ToUKTime();
            }
            else
            {
                await _dB.DocumentExpirys.AddAsync(new DocumentExpiry
                {
                    UserId = dto.UserId,
                    ExpiryDate = dto.ExpiryDate,
                    DocumentType = dto.DocType,
                    LastUpdatedOn = DateTime.Now.ToUKTime()
                });
            }

            await _dB.SaveChangesAsync();
        }

        public async Task<TurnDownSummaryDto> GetTurnDownsAsync(DateTime from, DateTime to)
        {
            var data = await _dB.TurnDowns
                .Where(o => o.DateTime >= from && o.DateTime <= to)
                .ToListAsync();

            return new TurnDownSummaryDto
            {
                Data = data,
                Total = data.Sum(o => o.Amount) ?? 0m,
                Count = data.Count
            };
        }

        public async Task<MessagingNotifyConfig?> GetMessageConfigAsync()
        {
            return await _dB.MessagingNotifyConfig
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task UpdateMessageConfigAsync(MessagingNotifyConfig req)
        {
            var existing = await _dB.MessagingNotifyConfig.FirstOrDefaultAsync();

            if (existing == null)
            {
                await _dB.MessagingNotifyConfig.AddAsync(req);
            }
            else
            {
                req.Id = existing.Id;
                _dB.Entry(existing).CurrentValues.SetValues(req);
            }

            await _dB.SaveChangesAsync();
        }

        public async Task<CompanyConfig?> GetCompanyConfigAsync()
        {
            return await _dB.CompanyConfig
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task UpdateCompanyConfigAsync(CompanyConfig req)
        {
            var existing = await _dB.CompanyConfig.FirstOrDefaultAsync();

            if (existing == null)
            {
                await _dB.CompanyConfig.AddAsync(req);
            }
            else
            {
                req.Id = existing.Id;
                _dB.Entry(existing).CurrentValues.SetValues(req);
            }

            await _dB.SaveChangesAsync();
        }

        public async Task UpdateBrowserFcmAsync(int userId, string? fcm)
        {
            await _dB.UserProfiles
                .Where(o => o.UserId == userId)
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.ChromeFCM, fcm));

            if (string.IsNullOrWhiteSpace(fcm))
            {
                return;
            }

            var existing = await _dB.Set<UserDeviceRegistration>()
                .FirstOrDefaultAsync(x => x.UserId == userId &&
                    x.DeviceType == UserDeviceType.BrowserPush &&
                    x.Token == fcm);

            if (existing == null)
            {
                await _dB.Set<UserDeviceRegistration>().AddAsync(new UserDeviceRegistration
                {
                    UserId = userId,
                    DeviceType = UserDeviceType.BrowserPush,
                    Token = fcm,
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
        }

        public async Task<List<string?>> GetBrowserFcmsAsync()
        {
            var data = await _dB.Set<UserDeviceRegistration>()
                .AsNoTracking()
                .Where(o => o.DeviceType == UserDeviceType.BrowserPush && o.IsActive)
                .Select(o => o.Token)
                .ToListAsync();

            await _messageService.SendBrowserNotification("test title", "test body");

            return data.Cast<string?>().ToList();
        }

        public async Task ClearBrowserFcmAsync(int userId)
        {
            await _dB.UserProfiles
                .Where(o => o.UserId == userId)
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.ChromeFCM, string.Empty));

            await _dB.Set<UserDeviceRegistration>()
                .Where(o => o.UserId == userId && o.DeviceType == UserDeviceType.BrowserPush)
                .ExecuteUpdateAsync(o => o
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.UpdatedUtc, DateTime.UtcNow));
        }

        public async Task<List<ActiveBookingAmendDto>> GetWebChangeRequestsAsync()
        {
            var requests = await _dB.WebAmendmentRequests
                .Where(o => !o.Processed)
                .AsNoTracking()
                .OrderBy(o => o.RequestedOn)
                .ToListAsync();

            var bookingIds = requests
                .Select(o => o.BookingId)
                .Distinct()
                .ToList();

            var bookings = await _dB.Bookings
                .Where(o => bookingIds.Contains(o.Id))
                .Select(o => new ActiveBookingDataDto
                {
                    BookingId = o.Id,
                    DateTime = o.PickupDateTime,
                    PickupAddress = $"{o.PickupAddress}, {o.PickupPostCode}",
                    DestinationAddress = $"{o.DestinationAddress}, {o.DestinationPostCode}",
                    PassengerName = o.PassengerName,
                    RecurranceId = o.RecurrenceID
                })
                .ToDictionaryAsync(o => o.BookingId);

            var result = new List<ActiveBookingAmendDto>();

            foreach (var item in requests)
            {
                if (!bookings.TryGetValue(item.BookingId, out var booking))
                {
                    continue;
                }

                result.Add(new ActiveBookingAmendDto
                {
                    Id = item.Id,
                    Amendments = item.Amendments,
                    CancelBooking = item.CancelBooking,
                    RequestedOn = item.RequestedOn,
                    BookingId = item.BookingId,
                    ApplyToBlock = item.ApplyToBlock,
                    DateTime = booking.DateTime,
                    PickupAddress = booking.PickupAddress,
                    DestinationAddress = booking.DestinationAddress,
                    PassengerName = booking.PassengerName,
                    RecurranceId = booking.RecurranceId
                });
            }

            return result;
        }

        public async Task UpdateWebChangeRequestAsync(int reqId)
        {
            await _dB.WebAmendmentRequests
                .Where(o => o.Id == reqId)
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.Processed, true));
        }
    }
}



