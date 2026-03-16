using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.User
{
    public class UserUpdateRequestDto
    {
        public int UserId { get; set; }

        [Required(), MinLength(3), MaxLength(50)]
        public string Fullname { get; set; } // code

        [Required]
        public AceRoles Role { get; set; }
        
        public string RegistrationNo { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Select a colour for this driver.")]
        public string ColorCode { get; set; }

        [Required]
        public bool ShowAllBookings { get; set; }

        public string VehicleMake { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleColor { get; set; }
        public VehicleType VehicleType { get; set; } = VehicleType.Unknown;
        public int CashCommissionRate { get; set; } = 0;
        // incorrect spelling from UI side
        public int CashCommisionRate { get; set; } = 0;
        public SendMessageOfType Comms { get; set; } = SendMessageOfType.WhatsApp;

        public bool NonAce { get; set; }
        }
}

