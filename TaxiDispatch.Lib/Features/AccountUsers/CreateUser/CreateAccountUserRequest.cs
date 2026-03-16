using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Features.AccountUsers.CreateUser;

public class CreateAccountUserRequest
{
    [Required]
    public int AccNo { get; set; }

    [Required, MinLength(3), MaxLength(50)]
    public string Fullname { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;
}
