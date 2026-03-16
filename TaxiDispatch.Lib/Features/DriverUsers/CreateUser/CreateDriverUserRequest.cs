using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Features.DriverUsers.CreateUser;

public class CreateDriverUserRequest
{
    [Required, MinLength(3), MaxLength(20)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(3), MaxLength(50)]
    public string Fullname { get; set; } = string.Empty;

    [Required]
    public string RegistrationNo { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ColorCode { get; set; } = string.Empty;

    public bool ShowAllBookings { get; set; }

    public bool ShowHvsBookings { get; set; }

    public string? VehicleMake { get; set; }

    public string? VehicleModel { get; set; }

    public string? VehicleColor { get; set; }

    public VehicleType VehicleType { get; set; } = VehicleType.Unknown;

    public SendMessageOfType Comms { get; set; } = SendMessageOfType.WhatsApp;

    public bool NonAce { get; set; }

    public int CashCommissionRate { get; set; } = 15;
}
