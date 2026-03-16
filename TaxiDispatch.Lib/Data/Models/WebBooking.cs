using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TaxiDispatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace TaxiDispatch.Data.Models
{
    public class WebBooking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int AccNo { get; set; }

        public DateTime PickupDateTime { get; set; }
        public bool ArriveBy { get; set; }
        public string? RecurrenceRule { get; set; } = string.Empty;

        [MaxLength(250)]
        public string PickupAddress { get; set; } = string.Empty;

        [StringLength(9, ErrorMessage = "Pickup postcode should not exceed 9 characters")]
        [MaxLength(9)]
        public string PickupPostCode { get; set; } = string.Empty;

        [MaxLength(250)]
        public string DestinationAddress { get; set; } = string.Empty;

        [StringLength(9, ErrorMessage = "Destination postcode should not exceed 9 characters")]
        [MaxLength(9)]
        public string DestinationPostCode { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Details { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? PassengerName { get; set; } = string.Empty;

        public int Passengers { get; set; } = 1;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Email { get; set; } = string.Empty;
        public BookingScope? Scope { get; set; }

        //
        public int? Luggage { get; set; }
        public int? DurationMinutes { get; set; }
        [Precision(18,2)]
        public decimal? Mileage { get; set; }
        public string? MileageText { get; set; }
        public string? DurationText { get; set; }
        public double? Price { get; set; }
        //

        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string AcceptedRejectedBy { get; set; } = string.Empty;
        public DateTime? AcceptedRejectedOn { get; set; }
        public string? RejectedReason { get; set; } = string.Empty;
        public bool Processed { get; set; } = false;
        public WebBookingStatus Status { get; set; } = WebBookingStatus.Default;
        
        [NotMapped]
        public string RepeatText { get; set; } = string.Empty;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

