using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class CancelBookingRequest
    {
        [Required]
        [Range(1, 999999)]
        public int BookingId { get; set; }

        public string CancelledByName { get; set; }

        public bool CancelBlock { get; set; }

        public bool CancelledOnArrival { get; set; }

        [Required]
        public int ActionByUserId { get; set; }

        public bool? SendEmail { get; set; }
    }
}
