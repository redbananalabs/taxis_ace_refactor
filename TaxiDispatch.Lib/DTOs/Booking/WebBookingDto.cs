#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class WebBookingDto : ModelValidator
    {
        [Required]
        public int AccNo { get; set; }
        [Required]
        public DateTime PickupDateTime { get; set; }
        public bool ArriveBy { get; set; }

        public string? RecurrenceRule { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string PickupAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(9, ErrorMessage = "Pickup postcode should not exceed 9 characters")]
        [MaxLength(9)]
        public string PickupPostCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string DestinationAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(9, ErrorMessage = "Destination postcode should not exceed 9 characters")]
        [MaxLength(9)]
        public string DestinationPostCode { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Details { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string? PassengerName { get; set; } = string.Empty;

        public int Passengers { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Email { get; set; } = string.Empty;
    }
}
