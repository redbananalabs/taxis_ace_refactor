using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Messaging;

public class SendTextMessageRequestDto
{
    [Required]
    public string Message { get; set; }

    [Required]
    [Phone]
    public string Telephone { get; set; }
}
