using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaxiDispatch.Modules.Messaging.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly MessagingConfig _config;
    private readonly ILogger<PushNotificationService> _logger;

    private static readonly ILoggerFactory _loggerFactory =
        LoggerFactory.Create(builder => builder.AddConsole());

    public PushNotificationService(IOptions<MessagingConfig> config)
    {
        _config = config.Value;
        _logger = _loggerFactory.CreateLogger<PushNotificationService>();
    }

    public async Task SendAndroidNotification(PushNotificationRequest req)
    {
        var customToken = FirebaseApp.DefaultInstance;

        foreach (var fcm in req.registration_ids)
        {
            var message = new Message
            {
                Notification = new Notification
                {
                    Title = req.notification.title,
                    Body = req.notification.body
                },
                Data = req.data,
                Token = fcm
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Push Message Sent: {Title} -> response = {Response}", req.notification.title, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                throw;
            }
        }
    }

    public async Task SendChromeNotification(PushNotificationRequest req)
    {
        var msg = new MulticastMessage
        {
            Tokens = req.registration_ids,
            Data = req.data,
            Notification = new Notification
            {
                Title = req.notification.title,
                Body = req.notification.body
            }
        };

        await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);
    }
}
