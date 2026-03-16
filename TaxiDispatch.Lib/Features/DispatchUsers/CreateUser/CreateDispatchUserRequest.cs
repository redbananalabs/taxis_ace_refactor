using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Features.DispatchUsers.CreateUser;

public class CreateDispatchUserRequest
{
    [Required, MinLength(3), MaxLength(20)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(50)]
    public string Fullname { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
