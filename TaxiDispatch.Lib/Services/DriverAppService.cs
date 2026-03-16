using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.DTOs.Driver.Responses;
using TaxiDispatch.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services;

public class DriverAppService : BaseService<DriverAppService>
{
    private readonly DispatchService _dispatchService;
    private readonly AceMessagingService _messagingService;
    private readonly UserActionsService _actionsService;

    public DriverAppService(
        TaxiDispatchContext dB,
        DispatchService dispatchService,
        AceMessagingService messagingService,
        UserActionsService actionsService,
        ILogger<DriverAppService> logger) : base(dB, logger)
    {
        _dispatchService = dispatchService;
        _messagingService = messagingService;
        _actionsService = actionsService;
    }

    public async Task<DriverAppProfileDto?> GetProfileAsync(int userId, string? fullName, string? telephone, string? email)
    {
        var profile = await _dB.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId);

        if (profile == null)
        {
            return null;
        }

        return new DriverAppProfileDto
        {
            Fullname = fullName,
            Telephone = telephone,
            Email = email,
            ColorCode = profile.ColorCodeRGB,
            VehicleReg = profile.RegNo,
            VehicleMake = profile.VehicleMake,
            VehicleModel = profile.VehicleModel,
            VehicleColour = profile.VehicleColour,
            FCM = profile.NotificationFCM,
            LastLogin = profile.LastLogin
        };
    }

    public async Task<List<Booking>> GetCompletedJobsAsync(int userId, DateTime from, DateTime to)
    {
        var data = await _dB.Bookings
            .Where(o => o.Status == BookingStatus.Complete &&
                        o.UserId == userId &&
                        o.PickupDateTime >= from.Date &&
                        o.PickupDateTime <= to &&
                        !o.Cancelled)
            .OrderByDescending(o => o.PickupDateTime)
            .AsNoTracking()
            .ToListAsync();

        data.ForEach(o =>
        {
            if (o.Mileage == null)
            {
                o.Mileage = 0;
            }
        });

        return data;
    }

    public async Task<bool> MarkArrivedAsync(int bookingId)
    {
        var booking = await _dB.Bookings
            .Where(o => o.Id == bookingId)
            .Select(o => new
            {
                o.UserId,
                o.DestinationAddress,
                o.PhoneNumber
            })
            .FirstOrDefaultAsync();

        if (booking == null ||
            string.IsNullOrEmpty(booking.PhoneNumber) ||
            !(booking.PhoneNumber.StartsWith("07") || booking.PhoneNumber.StartsWith("447")) ||
            booking.UserId == null)
        {
            return false;
        }

        var driver = await _dB.UserProfiles
            .Include(o => o.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == booking.UserId);

        if (driver?.User == null)
        {
            return false;
        }

        var driverName = driver.User.FullName ?? string.Empty;
        var firstName = driverName;

        if (!string.IsNullOrWhiteSpace(driverName) && driverName.Contains(' '))
        {
            firstName = driverName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? driverName;
        }

        _messagingService.SendCustomerArrivedSMS(
            booking.PhoneNumber,
            booking.DestinationAddress,
            driver.VehicleMake,
            driver.VehicleModel,
            driver.VehicleColour,
            driver.RegNo,
            firstName);

        await _dispatchService.UpdateJobStatus(driver.User.Id, bookingId, AppJobStatus.AtPickup);
        await _actionsService.LogBookingDriverArrived(bookingId, firstName);

        return true;
    }

    public async Task<bool> SetActiveJobAsync(int userId, int bookingId)
    {
        var status = await _dB.DriversOnShift.FirstOrDefaultAsync(o => o.UserId == userId);

        if (status == null)
        {
            return false;
        }

        status.ActiveBookingId = bookingId;
        _dB.DriversOnShift.Update(status);
        await _dB.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Found, int? ActiveBookingId)> GetActiveJobAsync(int userId)
    {
        var status = await _dB.DriversOnShift
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.UserId == userId);

        if (status == null)
        {
            return (false, null);
        }

        return (true, status.ActiveBookingId);
    }

    public async Task<List<DriverStatementHeaderDto>> GetStatementHeadersAsync(DateTime from, DateTime to, int userId)
    {
        return await _dB.DriverInvoiceStatements
            .Where(o => o.DateCreated >= from && o.DateCreated <= to && o.UserId == userId)
            .Select(o => new DriverStatementHeaderDto
            {
                StatementDate = o.DateCreated,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                TotalJobCount = o.TotalJobCount,
                StatementId = o.StatementId,
                SubTotal = o.SubTotal
            })
            .AsNoTracking()
            .ToListAsync();
    }
}
