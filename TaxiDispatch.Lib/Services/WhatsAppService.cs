using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;

namespace TaxiDispatch.Services;

public sealed class WhatsAppService
{
    private readonly TaxiDispatchContext _db;
    private readonly AceMessagingService _messaging;
    private readonly UserProfileService _userProfileService;
    private readonly UserActionsService _actions;
    private readonly UINotificationService _uiNotificationService;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        TaxiDispatchContext db,
        AceMessagingService messagingService,
        UserProfileService userProfileService,
        UserActionsService actions,
        UINotificationService uiNotificationService,
        ILogger<WhatsAppService> logger)
    {
        _db = db;
        _messaging = messagingService;
        _userProfileService = userProfileService;
        _actions = actions;
        _uiNotificationService = uiNotificationService;
        _logger = logger;
    }

    public async Task HandleReplyAsync(
        string from,
        string body,
        string? buttonPayload,
        CancellationToken cancellationToken = default)
    {
        var telephone = from.Replace("whatsapp:+44", "0", StringComparison.OrdinalIgnoreCase);
        var userId = await _userProfileService.GetUserFromPhoneNo(telephone);
        var payload = (buttonPayload ?? string.Empty).Replace("ACC", "").Replace("REJ", "");
        var combinedMessage = $"{body} - {payload}";

        var exists = await _db.DriverMessages
            .Where(o => o.Message.Contains(combinedMessage))
            .CountAsync(cancellationToken);

        if (exists == 0)
        {
            _db.DriverMessages.Add(new DriverMessage
            {
                UserId = userId,
                DateCreated = DateTime.Now.ToUKTime(),
                Message = combinedMessage,
                Read = false
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        var driverName = await _db.Users
            .Where(o => o.Id == userId)
            .Select(o => o.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(buttonPayload))
        {
            return;
        }

        _logger.LogInformation("WhatsApp reply received for driver {DriverId} with payload {Payload}", userId, buttonPayload);

        try
        {
            var jobId = Convert.ToInt32(payload);

            if (body == "ACCEPT")
            {
                var allocated = await _db.Bookings
                    .Where(o => o.Id == jobId)
                    .Select(o => o.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (allocated != null && allocated == userId)
                {
                    await _db.Bookings.Where(o => o.Id == jobId)
                        .ExecuteUpdateAsync(p => p.SetProperty(u => u.Status, BookingStatus.AcceptedJob), cancellationToken);

                    await _actions.LogBookingAccepted(jobId, driverName);
                }
            }
            else
            {
                await _db.Bookings.Where(o => o.Id == jobId)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(u => u.Status, BookingStatus.RejectedJob)
                        .SetProperty(u => u.UserId, (int?)null), cancellationToken);

                await _uiNotificationService.AddJobRejectNotification(userId, driverName, jobId);
                await _actions.LogBookingRejected(jobId, driverName);
            }

            await _db.DriverAllocations
                .Where(o => o.BookingId == jobId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying WhatsApp response.");
        }
    }

    public Task SendMessageAsync(string number, string message)
    {
        return _messaging.SendWhatsAppMessage(number, message);
    }
}
