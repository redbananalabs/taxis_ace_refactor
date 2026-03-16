using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class AllocateBookingDto : ModelValidator
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public int? UserId { get; set; }
        [Required]
        public int ActionByUserId { get; set; }
    }
}
