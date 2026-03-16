using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services
{
    public class UserActionsService : BaseService<UserActionsService>
    {
        private readonly UserProfileService _usersService;

        public UserActionsService(IDbContextFactory<TaxiDispatchContext> factory,
            UserProfileService usersService,ILogger<UserActionsService> logger) : base(factory,logger)
        {
            _usersService = usersService;
        }

        public async Task LogBookingCreated(int bookingId, string fullname, bool block)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
            };

            if (block)
            {
                obj.Action = "Block Booking Created";
            }
            else
            {
                obj.Action = "Booking Created";
            }

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingCancelled(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Booking Cancelled"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingCancellByDateRange(string fullname, string rangeData)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                ActionByUser = fullname,
                Action = $"Cancel Range ({rangeData})"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingUpdated(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Booking Updated"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingSoftAllocate(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Soft Allocated"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingSoftUnAllocate(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Soft Allocate (Removed)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingAllocated(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Allocated"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingUnAllocated(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "UnAllocated"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingCOA(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Marked as COA"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingDriverArrived(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Driver Arrived (Sent)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingPaymentLinkSent(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Payment Link (Sent)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingPaymentLinkReminderSent(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Payment Link Reminder (Sent)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingPaymentRefund(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Card Payment Refunded"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingPaymentReceiptSent(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Payment Receipt (Sent)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingSendConfirmationText(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Booking Confirmation (Sent)"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingCompleted(int bookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = fullname,
                Action = "Marked as Completed"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogSendQuote(string phone, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                ActionByUser = fullname,
                Action = $"Sent Quote to - {phone}"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogSendText(string phone, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                ActionByUser = fullname,
                Action = $"Sent Text to - {phone}"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogPaymentStatusUpdate(int bookingId)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = "REVOLUT",
                Action = "Payment Status Updated"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogTurnDown(int amount, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                ActionByUser = fullname,
                Action = $"Job Turned down £{amount}"
            };

            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingAccepted(int bookingId, string driverName)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = "System",
                Action = $"{driverName} Accepted Job"
            };
            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingRejected(int bookingId, string driverName)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = "System",
                Action = $"{driverName} Rejected Job"
            };
            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingRejectedTimeout(int bookingId, string driverName)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = "System",
                Action = $"{driverName} Job Offer Timed Out"
            };
            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task LogBookingResendAttempt (int bookingId, string driverName, int attempt)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = bookingId,
                ActionByUser = "System",
                Action = $"{driverName} Job Offer Resent ({attempt})"
            };
            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task<List<UserActionLog>> GetLogs()
        {
            var date  = DateTime.Now.ToUKTime().AddHours(-3);
            var res = await _dB.UserActionsLog.Where(o => o.Timestamp > date)
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            return res;
        }

        public async Task LogBookingMerged(int primaryBookingId, int appendBookingId, string fullname)
        {
            var obj = new UserActionLog
            {
                Timestamp = DateTime.Now.ToUKTime(),
                BookingId = primaryBookingId,
                ActionByUser = fullname,
                Action = $"{appendBookingId} was merged with {primaryBookingId}"
            };
            await _dB.UserActionsLog.AddAsync(obj);
            await _dB.SaveChangesAsync();
        }
    }
}


