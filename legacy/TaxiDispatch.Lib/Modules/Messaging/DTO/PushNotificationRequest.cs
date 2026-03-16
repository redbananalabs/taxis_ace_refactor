using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Messaging;

public class PushNotificationRequest
{
    public PushNotificationRequest()
    {
        registration_ids = new List<string>();
    }

    [Required]
    public NotificationMessageBody notification { get; set; }

    [Required]
    public Dictionary<string, string> data { get; set; }

    [Required]
    public List<string> registration_ids { get; set; }
}
