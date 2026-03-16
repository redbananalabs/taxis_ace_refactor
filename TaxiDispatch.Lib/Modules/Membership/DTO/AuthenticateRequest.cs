using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Membership;

public class AuthenticateRequest
{
    [Required]
    public string UserName { get; set; }

    [Required]
    public string Password { get; set; }
}
