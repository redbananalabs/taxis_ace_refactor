using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class UpdateBookingDateRequest
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public DateTime NewDate { get; set; }
        [Required]
        public string UpdatedByName { get; set; }
        [Required]
        public int ActionByUserId { get; set; }
    }
}
