using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Modules.Membership;

public class RegisterUserRequestDto
{
    public RegisterUserRequestDto()
    {
        RoleName = "User";
    }

    [Required, MinLength(3), MaxLength(20)]
    public string Username { get; set; }

    public string? Fullname { get; set; }

    [Required]
    public string PhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string RoleName { get; set; }
}
