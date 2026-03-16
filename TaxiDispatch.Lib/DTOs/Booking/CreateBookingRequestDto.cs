using TaxiDispatch.Domain;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class CreateBookingRequestDto : BookingModel
    {
        public int? UserId { get; set; }

        public DateTime? ReturnDateTime { get; set; }

        public bool Allocate { get; set; } = true;

        public bool IsDuplicate { get; set; }
    }
}
