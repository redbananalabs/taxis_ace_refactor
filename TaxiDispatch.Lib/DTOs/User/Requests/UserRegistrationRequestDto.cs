using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User.Requests
{
    public class UserRegistrationRequestDto
    {
        [Required, MinLength(3), MaxLength(20)]
        public string Username { get; set; }

        [Required, MinLength(3), MaxLength(50)]
        public string Fullname { get; set; }

        [Required]
        public AceRoles Role { get; set; }

        [Required]
        public string RegistrationNo { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required(ErrorMessage = "Select a colour for this driver.")]
        public string ColorCode { get; set; }

        [Required]
        public bool ShowAllBookings { get; set; }

        public string VehicleMake { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleColor { get; set; }

        public VehicleType VehicleType { get; set; } = VehicleType.Unknown;
        public SendMessageOfType Comms { get; set; } = SendMessageOfType.WhatsApp;
    }
}
