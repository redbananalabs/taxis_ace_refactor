using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Messaging;

public class NotificationMessageBody
{
    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    public string body { get; set; } = string.Empty;
}
