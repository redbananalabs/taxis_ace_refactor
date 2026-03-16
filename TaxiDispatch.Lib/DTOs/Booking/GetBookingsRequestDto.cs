using TaxiDispatch.Domain;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class GetBookingsRequestDto : ModelValidator
    {
        [Required]
        [DisplayName("From Date")]
        public DateTime From { get; set; }

        [DisplayName("To Date")]
        public DateTime? To { get; set; }
    }
}
