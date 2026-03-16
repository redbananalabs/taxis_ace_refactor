using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaxiDispatch.Services
{
    public class UINotificationService : BaseService<UINotificationService>
    {
        public UINotificationService(IDbContextFactory<TaxiDispatchContext> factory, ILogger<UINotificationService> logger) 
            : base(factory,logger)
        {

        }


        internal async Task AddDriverOfflineNotification(int userId, string fullname, int minutes)
        {
            // 1-3: convert and extract components
            var ts = TimeSpan.FromMinutes(minutes);
            var totalHours = (int)ts.TotalHours;
            var minutesPart = ts.Minutes;

            // 4: format as "hh:MM" (hours padded, minutes padded)
            var duration = $"{totalHours:00}hrs :{minutesPart:00}min";

            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"No Contact from '{userId} - {fullname}'\r\n\r\n" +
                // show formatted hh:MM and keep original minutes for clarity
                $"Their has been no GPS recieved for {duration}\r\n\r\n" +
                $"The last message or job was sent on WhatsApp or SMS";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;

            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task AddDocumentUploadNotification(int userId, string fullname, DocumentType type, string url)
        {
            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"New Document Upload From '{userId} - {fullname}'\r\n\r\n" +
                $"Doc Type: {type}\r\n\r\n" +
                $"Path: <a href=\"{url}\"> View Document</a>" +
                "Please review and update the drivers expiry date.";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;

            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task AddDocumentExpiredNotification(int userId, string fullname, DocumentType type)
        {
            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"Document Expired For '{userId} - {fullname}'\r\n\r\n" +
                $"Doc Type: {type}\r\n\r\n" +
                "Please review and update the drivers expiry date.";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;
            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task AddNoJobNotification(int userId, string fullname, int bookingId)
        {
            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"Driver '{userId} - {fullname}' has reported a 'NOJOB' for Booking #: {bookingId}";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;
            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task AddJobRejectNotification(int userId, string fullname, int bookingId)
        {
            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"Driver '{userId} - {fullname}' has rejected  Booking #: {bookingId}";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;
            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task AddJobRejectTimeoutNotification(int userId, string fullname, int bookingId)
        {
            var not = new UINotification();
            not.Status = NotificationStatus.Default;
            not.Message = $"Driver '{userId} - {fullname}' has not responded to the job offer. Reject (Timeout)  Booking #: {bookingId}";
            not.DateTimeStamp = DateTime.Now.ToUKTime();
            not.Event = NotificationEvent.Driver;

            var lastInserted = await _dB.UINotifications
                .OrderByDescending(e => e.Id).Select(o=>o.Message)
                .FirstOrDefaultAsync();

            if (lastInserted != null)
            {
                if (lastInserted != not.Message)
                {
                    await _dB.UINotifications.AddAsync(not);
                    await _dB.SaveChangesAsync();
                }
            }
        }
        


        #region WEB BOOKING NOTIFICATIONS
        public async Task WebBookingCreated(int accno)
        {
            var not = new UINotification
            {
                DateTimeStamp = DateTime.Now.ToUKTime(),
                Event = NotificationEvent.System,
                Status = NotificationStatus.Default,
                Message = $"Acccount {accno} has created a new web booking request.\r\n\r\nPlease review the request and 'Accept' or 'Reject' it."
            };

            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task BookingAmendmentRequest(string accountName, int bookingId, string message)
        {
            var not = new UINotification
            {
                DateTimeStamp = DateTime.Now.ToUKTime(),
                Event = NotificationEvent.System,
                Status = NotificationStatus.Default,
                Message = $"Acccount {accountName} has requested to amend booking id {bookingId} with the following changes:\r\n\r\n{message}."
            };

            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }

        public async Task BookingCancelationRequest(string accountName, int bookingId)
        {
            var not = new UINotification
            {
                DateTimeStamp = DateTime.Now.ToUKTime(),
                Event = NotificationEvent.System,
                Status = NotificationStatus.Default,
                Message = $"Acccount {accountName} has requested to cancel booking id {bookingId}."
            };

            await _dB.UINotifications.AddAsync(not);
            await _dB.SaveChangesAsync();
        }
        #endregion

        public async Task AddNotification(UINotification obj)
        { 
            await _dB.UINotifications.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task<List<UINotification>> GetNotifications()
        {
            var data = await _dB.UINotifications.Where(o=> o.Status == Domain.NotificationStatus.Default)
                .ToListAsync();

            return data;
        }

        public async Task MarkAsRead(int id)
        {
            await _dB.UINotifications.Where(o=>o.Id == id)
                .ExecuteUpdateAsync(o => o.SetProperty(p=>p.Status, Domain.NotificationStatus.Read));
        }


    }
}


