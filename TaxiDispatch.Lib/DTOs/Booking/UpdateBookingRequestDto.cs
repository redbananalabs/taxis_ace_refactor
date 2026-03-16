using TaxiDispatch.Domain;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class UpdateBookingRequestDto : CreateBookingRequestDto
    {
        [Required]
        public int BookingId { get; set; }
        public int? UserId { get; set; }

        public string UpdatedByName { get; set; }
        public bool EditBlock { get; set; }
    }
}
