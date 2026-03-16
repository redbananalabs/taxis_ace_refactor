using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class ManualPriceUpdateRequestDto : ModelValidator
    {
        [Required]
        [Range(1, 999999)]
        public int BookingId { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? PriceAccount { get; set; }

        [Required]
        public int ActionByUserId { get; set; }

        [Required]
        public string UpdatedByName { get; set; }
    }
}
