namespace TaxiDispatch.Modules.Messaging.Services;

public interface IPushNotificationService
{
    Task SendAndroidNotification(PushNotificationRequest request);
}
