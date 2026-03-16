using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Membership;

public class RefreshTokenRequest
{
    [Required]
    public string Token { get; set; }

    [Required]
    public string RefreshToken { get; set; }
}
