using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class CreateAvailabilityRequestDto : ModelValidator
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string From { get; set; }
        [Required]
        public string To { get; set; }
        public bool? GiveOrTake { get; set; } = false;
        [Required]
        public DriverAvailabilityType Type { get; set; }
        public string? Note { get; set; }
    }
}
