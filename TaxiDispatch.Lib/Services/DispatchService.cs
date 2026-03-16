#nullable disable
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.Booking;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiDispatch.Modules.Messaging;


namespace TaxiDispatch.Services
{
    public class DispatchService : BaseService<DispatchService>
    {
        private readonly IMapper _mapper;
        private readonly UINotificationService _uiNotificationService;
        private readonly UserActionsService _userActionsService;
        private readonly UserProfileService _usersService;
        private readonly AceMessagingService _messageService;
        private readonly AvailabilityService _availabilityService;

        public DispatchService(IDbContextFactory<TaxiDispatchContext> factory,
            IMapper mapper,
            AvailabilityService availabilityService,
            UserProfileService usersService,
            UINotificationService uINotificationService,
            UserActionsService userActionsService,
            AceMessagingService messagingService, ILogger<DispatchService> logger) : base(factory, logger)
        {
            _mapper = mapper;
            _usersService = usersService;
            _messageService = messagingService;
            _availabilityService = availabilityService;
            _uiNotificationService = uINotificationService;
            _userActionsService = userActionsService;
        }
        
        public async Task<Result> AllocateBooking(AllocateBookingDto dto)
        {
            var booking = await _dB.Bookings.Where(o => o.Id == dto.BookingId).Include(o=>o.Vias).AsNoTracking().FirstOrDefaultAsync();

            if (booking != null)
            {
                var fcm = "";
                var tel = "";

                int? userid = dto.UserId > 0 ? dto.UserId : null;

                // get user fcm token
                if (userid != null)
                {
                    fcm = await _usersService.GetFCMToken(userid.Value);
                    tel = await _usersService.GetPhoneNumber(userid.Value);
                }

                if (booking.UserId != null) // inform previous driver
                {
                    if (booking.UserId != dto.UserId) // check we are not allocating job to same driver
                    {
                        await DoJobUnallocatedMessages(booking, (int)booking.UserId);

                        await _dB.Bookings.Where(o => o.Id == booking.Id)
                            .ExecuteUpdateAsync(o =>
                                o.SetProperty(u => u.Status, BookingStatus.None)
                            .SetProperty(u => u.SuggestedUserId, default(int?)));

                        await _dB.JobOffers.Where(o => o.BookingId == booking.Id)
                            .ExecuteDeleteAsync();
                    }
                }

                var date = DateTime.Now.ToUKTime();

                await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                    .ExecuteUpdateAsync(o =>
                        o.SetProperty(u => u.UserId, userid)
                        .SetProperty(u => u.Status, BookingStatus.None)
                        .SetProperty(u => u.AllocatedById, dto.ActionByUserId)
                        .SetProperty(u => u.AllocatedAt, date));

                // create audit log

                // get users name
                var fname = await _dB.Users.Where(o => o.Id == dto.ActionByUserId).Select(o => o.FullName).FirstOrDefaultAsync();

                await _dB.BookingChangeAudits.AddAsync(new BookingChangeAudit
                {
                    EntityIdentifier = booking.Id.ToString(),
                    UserFullName = fname,
                    TimeStamp = date,
                    PropertyName = "Allocated",
                    OldValue = booking.UserId.ToString(),
                    NewValue = dto.UserId.ToString(),
                    Action = "Amended",
                    EntityName = "Booking"
                });

                await _dB.SaveChangesAsync();

                _logger.LogInformation($"job id: {dto.BookingId} allocated to '{userid}' by userid {dto.UserId}");

                _logger.LogInformation($"Allocating booking id {booking.Id} to userId {userid}");

                if (userid != null)
                {
                    // send messages
                    await DoJobAllocateMessages(booking, userid.Value);
                }

                return Result.Ok();
            }

            return Result.Fail("The booking was not allocated. booking not found");
        }

        public async Task Complete(CompleteJobRequestDto req, int id, bool isAdmin)
        {
            _logger.LogInformation("Complete packet received: {@Req}", req);

            // Get the pickup time (nullable in case the booking isn't found)
            var pickup = await _dB.Bookings
                .Where(b => b.Id == req.BookingId)
                .Select(b => (DateTime?)b.PickupDateTime)
                .FirstOrDefaultAsync();

            if (pickup is null)
            {
                _logger.LogWarning("Booking {BookingId} not found or has no pickup time.", req.BookingId);
                return;
            }

            // Prefer consistent clock and timezone; use Utc if your DB stores UTC
            var now = DateTime.Now.ToUKTime();           // or DateTime.Now if you truly store local times
            var pickupUtc = pickup.Value;        // assume stored as UTC

            // Block if we're before the pickup time (job is in the future)
            if (now < pickupUtc)
            {
                _logger.LogInformation("Cannot complete before pickup time. Now: {Now}, Pickup: {Pickup}", now, pickupUtc);
                return;
            }

            // Otherwise, it's at/after pickup time — allow completion
            _logger.LogInformation("Proceeding to complete booking {BookingId}.", req.BookingId);

            await _dB.DriversOnShift
                .Where(o => o.UserId == id)
                .ExecuteUpdateAsync(o => o.SetProperty(p => p.ActiveBookingId, p => null));

            await _dB.Bookings.Where(o => o.Id == req.BookingId)
                .ExecuteUpdateAsync(u =>
                u.SetProperty(o => o.WaitingTimeMinutes, req.WaitingTime)
                .SetProperty(o => o.ParkingCharge, (decimal)req.ParkingCharge)
                .SetProperty(o => o.Price, (decimal)req.DriverPrice)
                .SetProperty(o=> o.Tip, (decimal)req.Tip)
                .SetProperty(o => o.Status, BookingStatus.Complete));

            // stops accoutn price being reset to 0
            if (isAdmin)
            {
                await _dB.Bookings.Where(o => o.Id == req.BookingId)
                    .ExecuteUpdateAsync(u => u.SetProperty(o => o.PriceAccount, (decimal)req.AccountPrice));
            }

            var obj = await _dB.Bookings
                .Where(o => o.Id == req.BookingId)
                .Select(o => new { o.PhoneNumber, o.Scope })
                .FirstOrDefaultAsync();

            var customerMessage = await _messageService.GetMessageTypeToSend(SentMessageType.CustomerOnComplete);

            if (obj.Scope == BookingScope.Cash)
            {
                await _dB.Bookings.Where(o => o.Id == req.BookingId)
                .ExecuteUpdateAsync(u => u.SetProperty(o => o.PaymentStatus, PaymentStatus.Paid));
            }

            if (!string.IsNullOrEmpty(obj.PhoneNumber) && (obj.Scope == BookingScope.Cash 
                || obj.Scope == BookingScope.Card))
            {
                try
                {
                    var phone = obj.PhoneNumber.Replace(" ", "");
                    _messageService.SendCustomerOnBookingCompletedSMS(phone);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending customer allocate message using rabbit");
                }
            }

            await RemoveActiveJob(req.BookingId);
        }

        public async Task DriverShift(int userid, AppDriverShift status)
        {
            DriverOnShift obj;
            var date = DateTime.Now.ToUKTime();
            var exists = await _dB.DriversOnShift.CountAsync(o => o.UserId == userid) == 1 ? true : false;
            
            var entry = new DriverShiftLog { TimeStamp = date, UserId = userid };

            if (!exists)
            {
                obj = new DriverOnShift { UserId = userid };

                await _dB.DriversOnShift.AddAsync(obj);
                await _dB.SaveChangesAsync();
            }
            else
            {
                obj = await _dB.DriversOnShift.FirstOrDefaultAsync(o => o.UserId == userid);
            }

            switch (status)
            {
                case AppDriverShift.Start:
                    {
                        obj.StartAt = date;
                        obj.Status = AppJobStatus.Clear;
                        obj.OnBreak = false;
                        obj.ClearAt = date;

                        // log entry
                        entry.EntryType = AppDriverShift.Start;
                    }
                    break;
                case AppDriverShift.Finish:
                    {
                        _dB.DriversOnShift.Remove(obj);
                        
                        // log entry
                        entry.EntryType = AppDriverShift.Finish;
                    }
                    break;
                case AppDriverShift.OnBreak:
                    {
                        obj.OnBreak = true;
                        obj.BreakStartAt = date;

                        // log entry
                        entry.EntryType = AppDriverShift.OnBreak;
                    }
                    break;
                case AppDriverShift.FinishBreak:
                    {
                        obj.OnBreak = false;
                        obj.BreakStartAt = null;

                        // log entry
                        entry.EntryType = AppDriverShift.FinishBreak;
                    }
                    break;
            }

            if(status != AppDriverShift.Finish)
                _dB.DriversOnShift.Update(obj);

            // add to log
            await _dB.DriversShiftLogs.AddAsync(entry);

            // save all changes
            await _dB.SaveChangesAsync();
        }

        public async Task AcceptReject(int userid, string fullname, int jobno, AppJobOffer response)
        {
            if (response == AppJobOffer.Accept)
            {
                await _dB.Bookings.Where(o => o.Id == jobno).ExecuteUpdateAsync(p =>
                        p.SetProperty(u => u.Status, BookingStatus.AcceptedJob));

            }
            else if (response == AppJobOffer.Reject)
            {
                await _dB.Bookings.Where(o => o.Id == jobno).ExecuteUpdateAsync(p =>
                            p.SetProperty(u => u.Status, BookingStatus.RejectedJob)
                            .SetProperty(u=>u.UserId, (int?)null));

                await _uiNotificationService.AddJobRejectNotification(userid,fullname,jobno);
            }
            else // time out
            {
                await _dB.Bookings.Where(o => o.Id == jobno).ExecuteUpdateAsync(p =>
                            p.SetProperty(u => u.Status, BookingStatus.RejectedJobTimeout)
                            .SetProperty(u => u.UserId, (int?)null));

                await _uiNotificationService.AddJobRejectTimeoutNotification(userid, fullname, jobno);
            }
        }

        public async Task UpdateJobStatus(int userid, int jobno, AppJobStatus status)
        {
            var obj = await _dB.DriversOnShift.FirstOrDefaultAsync(o => o.UserId == userid);

            if (obj != null)
            {
                if (status == AppJobStatus.Clear)
                {
                    await RemoveActiveJob(jobno);
                }
                else if (status == AppJobStatus.NoJob)
                {
                    await RemoveActiveJob(jobno);
                    
                    // notify ui
                    var fullname = await _usersService.GetFullname(userid);
                    await _uiNotificationService.AddNoJobNotification(userid,fullname, jobno);
                }
                else
                {
                    obj.ActiveBookingId = jobno;
                }

                obj.Status = status;

                _dB.DriversOnShift.Update(obj);
                await _dB.SaveChangesAsync();
            }
        }

        #region MESSAGES
        public async Task DoJobAllocateMessages(Booking booking, int userid)
        {
            #region DRIVER MESSAGE
            var driverMessageType = await _messageService.GetMessageTypeToSend(SentMessageType.DriverOnAllocate);

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

            var fname = await _usersService.GetFullname(userid);

            var online = await IsPushOnline(userid);

            if (del == SendMessageOfType.Push && !string.IsNullOrEmpty(fcm) && online.Alive)
            {
                _logger.LogInformation($"UserId {userid} has FCM token - .");

                var navObj = new Dictionary<string, string>();
                navObj.Add("NavId", $"{(int)PushNotificationNavId.Allocate}");
                navObj.Add("bookingId", booking.Id.ToString());
                navObj.Add("datetime", booking.PickupDateTime.ToString("dd/MM/yyyy HH:mm"));
                navObj.Add("asap", booking.IsASAP.ToString());
                navObj.Add("pickup", $"{booking.PickupAddress.ToString()}, {booking.PickupPostCode}");

                if (booking.Vias?.Count > 0)
                {
                    for (int i = 0; i < booking.Vias.Count; i++)
                    {
                        navObj.Add($"via-{i}", $"{booking.Vias[i].Address.ToString()}, {booking.Vias[i].PostCode}");
                    }
                }

                navObj.Add("drop", $"{booking.DestinationAddress.ToString()}, {booking.DestinationPostCode}");
                navObj.Add("passenger", booking.PassengerName.ToString());
                navObj.Add("scope", booking.Scope.ToString());
                navObj.Add("price", booking.Price.ToString());

                var asap = booking.IsASAP ? "(ASAP)" : "";

                var body = $"{booking.PickupDateTime.ToString("dd/MM/yy HH:mm")} {asap}\r\n"
                        + $"{booking.PickupAddress}, {booking.PickupPostCode}\r\n"
                        + $"\r\nDropping:\r\n{booking.DestinationAddress},{booking.DestinationPostCode}";

                // create offer entry
                var guid = await CreateJobOfferEntry(userid, "Job Offer", body, navObj, booking.PickupDateTime, booking.Id);
                var data = new Dictionary<string, string>
                     {
                        { "NavId", $"{(int)PushNotificationNavId.Allocate}" },
                        { "guid", guid }
                    };

                var pushn = new PushNotificationRequest
                {
                    notification = new NotificationMessageBody
                    {
                        title = "Job Offer.",
                        body = body
                    },
                    data = data,
                    registration_ids = new List<string> { fcm }
                };

                // send
                try
                {
                    await _messageService.SendAndroidNotification(pushn);

                    _logger.LogInformation($"Push notification sent to userId {userid} with FCM token {fcm}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Push notification sent to userId {userid} with FCM token {fcm} failed");
                }
            }
            else // Whatsapp || SMS
            {
                if (!online.Alive && del == SendMessageOfType.Push)
                {
                    await _uiNotificationService.AddDriverOfflineNotification(userid,fname,online.Minutes);
                }

                if (driverMessageType == SendMessageOfType.WhatsApp)
                {
                    if (!string.IsNullOrEmpty(tel))
                    {
                        var hasVias = "No";
                        var hasDetails = "No";
                        var passengerName = "-";

                        if (booking.Vias != null)
                        {
                            //  hasVias = booking.Vias.Any() ? "Yes" : "No";
                            if (booking.Vias.Count > 0)
                            {
                                hasVias = "Yes";
                            }
                        }

                       

                        if (!string.IsNullOrEmpty(booking.Details))
                        {
                            hasDetails = "Yes";
                        }

                        if (!string.IsNullOrEmpty(booking.PassengerName))
                        {
                            passengerName = booking.PassengerName;
                        }

                        await _messageService.SendWhatsAppAllocatedV3(tel, booking.PickupDateTime, passengerName, booking.Passengers,
                            $"{booking.PickupAddress}, {booking.PickupPostCode}", $"{booking.DestinationAddress}, {booking.DestinationPostCode}", hasVias, hasDetails, booking.Id, userid);
                    }
                }
                else if (driverMessageType == SendMessageOfType.Sms)
                {
                    // Not Implemented
                }
            }
            #endregion

            #region CUSTOMER MESSAGE

            var customerMessageType = await _messageService.GetMessageTypeToSend(SentMessageType.CustomerOnAllocate);

            if (!string.IsNullOrEmpty(booking.PhoneNumber) && (booking.Scope == BookingScope.Cash || booking.Scope == BookingScope.Card))
            {
                if (customerMessageType == SendMessageOfType.Sms)
                {
                    try
                    {
                        var phone = booking.PhoneNumber.Replace(" ", "");
                        var uname = await _usersService.GetUsername(userid);
                        var profile = await _usersService.GetProfile(uname);

                        _messageService.SendCustomerOnAllocateSMS(phone, profile.VehicleMake, profile.VehicleModel, profile.VehicleColour, profile.RegNo, profile.User.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending customer allocate message using rabbit");
                    }
                }
            }
            #endregion
        }

        public async Task DoJobAmendedMessages(Booking booking, int? userid)
        {
            var now = DateTime.Now.ToUKTime();

            if (booking.PickupDateTime <= now.AddMinutes(15))
            {
                _logger.LogInformation(
                    $"Booking pickup time {booking.PickupDateTime} has already passed or is too soon. No amend messages sent."
                );
                return;
            }

            #region DRIVER MESSAGE

            if (!userid.HasValue)
                return;

            var fcm = "";
            var tel = "";
            var del = SendMessageOfType.WhatsApp;
            var nonace = await _usersService.IsNonAce(userid.Value);

            // get user fcm token
            if (userid != null)
            {
                fcm = await _usersService.GetFCMToken(userid.Value);
                tel = await _usersService.GetPhoneNumber(userid.Value);
                del = await _usersService.GetCommsPlatform(userid.Value);
            }

            var fname = await _usersService.GetFullname(userid.Value);

            var online = await IsPushOnline(userid.Value);

            if (del == SendMessageOfType.Push && !string.IsNullOrEmpty(fcm) && online.Alive)
            {
                _logger.LogInformation($"UserId {userid} has FCM token - .");

                var date = DateTime.Now.ToUKTime();

                var navObj = new Dictionary<string, string>
                            {
                                { "NavId", $"{(int)PushNotificationNavId.Amended}" },
                                { "bookingId", $"{booking.Id}" },
                                { "pickupdatetime", booking.PickupDateTime.ToString("dd/MM/yyyy HH:mm")},
                                { "passenger",$"{booking.PassengerName}"}
                            };

                var pushn = new PushNotificationRequest
                {
                    notification = new NotificationMessageBody
                    {
                        title = "BOOKING AMENDED",
                        body = $"Your booking on {booking.PickupDateTime.ToString("dd/MM/yyyy HH:mm")} for passenger {booking.PassengerName} has been amended."
                    },
                    data = navObj,
                    registration_ids = new List<string> { fcm }
                };

                // send
                try
                {
                    await _messageService.SendAndroidNotification(pushn);

                    _logger.LogInformation($"Push notification sent to userId {userid} with FCM token {fcm}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Push notification sent to userId {userid} with FCM token {fcm} failed");
                }
            }
            else
            {
                
                if (!online.Alive && del == SendMessageOfType.Push)
                {
                    await _uiNotificationService.AddDriverOfflineNotification(userid.Value,fname,online.Minutes);
                }

                var date = DateTime.Now.ToUKTime();

                if (!string.IsNullOrEmpty(tel))
                {
                    var passengerName = "-";

                    if (!string.IsNullOrEmpty(booking.PassengerName))
                    {
                        passengerName = booking.PassengerName;
                    }

                    var driverMessage = await _messageService.GetMessageTypeToSend(SentMessageType.DriverOnAmend);

                    if (driverMessage == SendMessageOfType.WhatsApp)
                    {
                        if (booking.PickupDateTime >= date)
                        {
                            await _messageService.SendWhatsAppBookingAmended(passengerName, booking.PickupDateTime, tel);

                        }
                    }
                    else if (driverMessage == SendMessageOfType.Sms)
                    {
                        if (!booking.CancelledOnArrival)
                        {
                            if (booking.PickupDateTime >= date)
                            {

                                _messageService.SendDriverBookingAmendedSMS(tel, booking.PickupDateTime.ToString("dd/MM/yy HH:mm"), passengerName,nonace);
                            }
                        }
                    }
                }
            }

            #endregion
        }

        public async Task DoJobUnallocatedMessages(Booking booking, int userid)
        {
            #region DRIVER MESSAGE
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

            var fname = await _usersService.GetFullname(userid);

            var online = await IsPushOnline(userid);

            if (del == SendMessageOfType.Push && !string.IsNullOrEmpty(fcm) && online.Alive)
            {
                _logger.LogInformation($"UserId {userid} has FCM token - notify (Unallocate).");

                var navObj = new Dictionary<string, string>
                            {
                                { "NavId", $"{(int)PushNotificationNavId.Unallocate}" },
                                { "bookingId", booking.Id.ToString() },
                                { "datetime", booking.PickupDateTime.ToString("dd/MM/yyyy HH:mm") },
                                { "asap", booking.IsASAP.ToString() },
                                { "passenger", booking.PassengerName.ToString() }
                            };

                var pushn = new PushNotificationRequest
                {
                    notification = new NotificationMessageBody
                    {
                        title = "Job Unallocated.",
                        body = $"{booking.PickupDateTime.ToString("dd/MM/yy HH:mm")}\r\n"
                               + $"{booking.PickupAddress}, {booking.PickupPostCode}\r\n"
                               + $"\r\nDropping:\r\n{booking.DestinationAddress},{booking.DestinationPostCode}"
                    },
                    data = navObj,
                    registration_ids = new List<string> { fcm }
                };

                // send
                try
                {
                    await _messageService.SendAndroidNotification(pushn);

                    _logger.LogInformation($"Push notification sent to userId {userid} with FCM token {fcm}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Push notification sent to userId {userid} with FCM token {fcm} failed");
                }
            }
            else
            {
                if (!online.Alive && del == SendMessageOfType.Push)
                {
                    await _uiNotificationService.AddDriverOfflineNotification(userid, fname, online.Minutes);
                }

                var driverMessageType = await _messageService.GetMessageTypeToSend(SentMessageType.DriverOnUnAllocate);

                if (driverMessageType == SendMessageOfType.WhatsApp)
                {
                    if (!string.IsNullOrEmpty(tel))
                    {
                        var passengerName = "-";

                        if (!string.IsNullOrEmpty(booking.PassengerName))
                        {
                            passengerName = booking.PassengerName;
                        }

                        await _messageService.SendWhatsAppUnAllocated(passengerName, booking.PickupDateTime, tel);
                    }
                }
                else if (driverMessageType == SendMessageOfType.Sms)
                {
                    _messageService.SendDriverBookingUnallocatedSMS(tel, booking.PickupDateTime.ToString("dd/MM/yy HH:mm"), booking.PassengerName);
                }
            }
            #endregion

            #region CUSTOMER MESSAGE
            // NOT IMPLEMENTED
            #endregion
        }

        public async Task DoJobCancelledMessages(Booking booking, int? userid)
        {
            #region DRIVER MESSAGE

            if (!userid.HasValue)
                goto LABEL_001;

            var driverMessageType = await _messageService.GetMessageTypeToSend(SentMessageType.DriverOnCancel);

            var fcm = "";
            var tel = "";
            var del = SendMessageOfType.WhatsApp;
            var nonace = await _usersService.IsNonAce(userid.Value);

            // get user fcm token
            if (userid.HasValue && userid != 0)
            {
                fcm = await _usersService.GetFCMToken(userid.Value);
                tel = await _usersService.GetPhoneNumber(userid.Value);
                del = await _usersService.GetCommsPlatform(userid.Value);
            }

            var fname = await _usersService.GetFullname(userid.Value);

            var online = await IsPushOnline(userid.Value);

            if (del == SendMessageOfType.Push && !string.IsNullOrEmpty(fcm) && online.Alive)
            {
                _logger.LogInformation($"UserId {userid} has FCM token - .");

                var date = DateTime.Now.ToUKTime();

                var navObj = new Dictionary<string, string>
                            {
                                { "NavId", $"{(int)PushNotificationNavId.Cancelled}" },
                                { "bookingId", $"{booking.Id}" },
                                { "pickupdatetime", booking.PickupDateTime.ToString("dd/MM/yyyy HH:mm")},
                                { "passenger",$"{booking.PassengerName}"}
                            };

                var pushn = new PushNotificationRequest
                {
                    notification = new NotificationMessageBody
                    {
                        title = "Booking Cancelled",
                        body = $"Your booking on {booking.PickupDateTime.ToString("dd/MM/yyyy HH;mm")} for passenger {booking.PassengerName} has been cancelled."
                    },
                    data = navObj,
                    registration_ids = new List<string> { fcm }
                };

                // send
                try
                {
                    await _messageService.SendAndroidNotification(pushn);

                    _logger.LogInformation($"Push notification sent to userId {userid} with FCM token {fcm}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Push notification sent to userId {userid} with FCM token {fcm} failed");
                }
            }
            else
            {
                if (!online.Alive && del == SendMessageOfType.Push)
                {
                    await _uiNotificationService.AddDriverOfflineNotification(userid.Value, fname, online.Minutes);
                }

                if (driverMessageType == SendMessageOfType.WhatsApp)
                {
                    await _messageService.SendWhatsAppCancelled(booking.PassengerName, booking.PickupDateTime, tel);
                }
                else if (driverMessageType == SendMessageOfType.Sms)
                {
                    _messageService.SendDriverBookingCancelledSMS(tel, booking.PickupDateTime.ToString("dd/MM/yy HH:mm"), booking.PassengerName, nonace);
                }
            }
        #endregion

        #region CUSTOMER MESSAGE
        LABEL_001:
            var customerMessageType = await _messageService.GetMessageTypeToSend(SentMessageType.CustomerOnCancel);

            if (!string.IsNullOrEmpty(booking.PhoneNumber) &&
                (booking.Scope == BookingScope.Cash ||
                booking.Scope == BookingScope.Card))
            {
                if (customerMessageType == SendMessageOfType.Sms)
                {
                    try
                    {
                        var phone = booking.PhoneNumber.Replace(" ", "");
                        _messageService.SendCustomerOnBookingCancelledSMS(phone, booking.PickupDateTime.ToString("dd/MM/yy HH:mm"), booking.PickupAddress);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending customer allocate message using rabbit");
                    }
                }
            }

            #endregion
        }
        #endregion

        #region Job Offer Logic
        public async Task<string> CreateJobOfferEntry(int userid, string title, string body, Dictionary<string, string> data, DateTime dueAt,int bookingId)
        {
            // delete if we have an entry for this bookingId before we create another
            await _dB.JobOffers
                .Where(o=>o.BookingId == bookingId)
                .ExecuteDeleteAsync();


            var offer = new JobOffer { Body = body, Data = data, Title = title, UserId = userid, BookingDateTime = dueAt, BookingId = bookingId };
            offer.CreatedOn = DateTime.Now.ToUKTime();
            offer.Guid = Guid.NewGuid().ToString();

            await _dB.JobOffers.AddAsync(offer);
            await _dB.SaveChangesAsync();

            return offer.Guid;
        }

        public async Task<JobOffer?> GetJobOfferEntry(string guid)
        {
            return await _dB.JobOffers.AsNoTracking().Where(o => o.Guid == guid).FirstOrDefaultAsync();
        }

        public async Task<List<JobOffer>> GetJobOffers(int userId)
        {
            return await _dB.JobOffers.AsNoTracking().Where(o => o.UserId == userId).ToListAsync();
        }


        public async Task RefreshJobOffers()
        {
            var offers = await _dB.JobOffers.ToListAsync();
            var now = DateTime.Now.ToUKTime();

            var exp1 = 30;
            var exp2 = 60;
            var exp3 = 180;

            foreach (var offer in offers)
            {
                var timeUntilBooking = (offer.BookingDateTime - now).TotalMinutes;

                // --------------------------------------------------------
                // HANDLE JOBS WHERE PICKUP TIME IS IN THE PAST
                // --------------------------------------------------------
                if (timeUntilBooking < 0)
                {
                    _logger.LogInformation(
                        $"RefreshJobOffers: BookingId {offer.BookingId} pickup time ({offer.BookingDateTime}) is in the past. Removing job offer."
                    );

                    _dB.JobOffers.Remove(offer);
                    continue;
                }

                int timeoutSeconds;

                if (timeUntilBooking <= exp1)
                {
                    timeoutSeconds = 60 * 5;
                }
                else if (timeUntilBooking <= exp2)
                {
                    timeoutSeconds = 60 * 10;
                }
                else if (timeUntilBooking <= exp3)
                {
                    timeoutSeconds = 60 * 15;
                }
                else
                {
                    timeoutSeconds = 60 * 360; // 6 hours
                }

                // FUTURE JOB, DO NOT EXPIRE YET
                if (timeUntilBooking > exp3)
                {
                    if ((now - offer.CreatedOn).TotalSeconds <= timeoutSeconds)
                    {
                        _logger.LogInformation($"JobOffer {offer.BookingId} not expired yet within {exp3}. Skipping...");
                        continue;
                    }
                }

                if (timeUntilBooking > exp2)
                {
                    if ((now - offer.CreatedOn).TotalSeconds <= timeoutSeconds)
                    {
                        _logger.LogInformation($"JobOffer {offer.BookingId} not expired yet within {exp2}. Skipping...");
                        continue;
                    }
                }

                if (timeUntilBooking > exp1)
                {
                    if ((now - offer.CreatedOn).TotalSeconds <= timeoutSeconds)
                    {
                        _logger.LogInformation($"JobOffer {offer.BookingId} not expired yet within {exp1}. Skipping...");
                        continue;
                    }
                }

                var isExpired = (now - offer.CreatedOn).TotalSeconds > timeoutSeconds;
                var fullname = _dB.Users.Where(o => o.Id == offer.UserId)
                                        .Select(o => o.FullName)
                                        .FirstOrDefault();

                if (!isExpired)
                    continue;

                // --------------------------------------------------------
                // EXPIRED LOGIC
                // --------------------------------------------------------
                if (offer.Attempts >= 3)
                {
                    _logger.LogInformation("RefreshJobOffers: Removing JobOffer");
                    _dB.JobOffers.Remove(offer);

                    var jobno = Convert.ToInt32(offer.Data["bookingId"]);

                    await _messageService.SendBrowserNotification("Job Offer (Timed Out)",
                        $"{fullname} did not respond to the job offer within the allowed timeframe.");

                    await _uiNotificationService.AddJobRejectTimeoutNotification(offer.UserId, fullname, jobno);
                    await AcceptReject(offer.UserId, fullname, jobno, AppJobOffer.TimedOut);
                    await _userActionsService.LogBookingRejectedTimeout(jobno, fullname);

                    _logger.LogInformation($"RefreshJobOffers: Job Auto Rejected - Offer Deleted - BookingId: {offer.BookingId}");
                }
                else
                {
                    // RESEND
                    _logger.LogInformation($"RefreshJobOffers: Attempts [{offer.Attempts}] Resending JobOffer - BookingId: {offer.BookingId}");

                    offer.Attempts++;
                    _dB.JobOffers.Update(offer);
                    await _dB.SaveChangesAsync();

                    var fcm = await _usersService.GetFCMToken(offer.UserId);

                    var pushn = new PushNotificationRequest
                    {
                        notification = new NotificationMessageBody
                        {
                            title = $"Job Offer (R{offer.Attempts}).",
                            body = offer.Body
                        },
                        data = offer.Data,
                        registration_ids = new List<string> { fcm }
                    };

                    try
                    {
                        await _messageService.SendAndroidNotification(pushn);
                        await _userActionsService.LogBookingResendAttempt(offer.BookingId, fullname, offer.Attempts);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Push resend failed for userId {offer.UserId}");
                    }
                }
            }

            await _dB.SaveChangesAsync();
        }

        public async Task DeleteJobOfferEntry(string guid)
        {
            await _dB.JobOffers.Where(o => o.Guid == guid).ExecuteDeleteAsync();
        }
        #endregion

        public async Task RemoveActiveJob(int bookingId)
        {
            await _dB.DriversOnShift.Where(o => o.ActiveBookingId == bookingId)
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.ActiveBookingId, (int?)null)
                .SetProperty(u => u.Status, AppJobStatus.Clear));
        }

        private async Task<(bool Alive, int Minutes)> IsPushOnline(int userId)
        { 
            var time = await _dB.UserProfiles
                .Where(o=>o.UserId == userId)
                .Select(o=>o.GpsLastUpdated)
                .FirstOrDefaultAsync();

            if (time == null) 
                return (false,99);

            // Calculate the time difference
            TimeSpan difference = DateTime.Now.ToUKTime() - time.Value;

            // If you want whole minutes only:
            int diff = (int)difference.TotalMinutes;
            var off = diff <= 1;


            // TEMP OVERRIDE FOR TESTING
            return (true, 0);

           // return (off,diff);
        }
    }
}




