#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class BookingModel : ModelValidator
    {
        public BookingModel()
        {
            BookedByName = string.Empty;
            PassengerName = string.Empty;
            BookedByName = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
        }

        public DateTime DateCreated { get; set; }

        // interface members
        [JsonProperty]
        [Display(Prompt = "Booked By", Name = "BookdBy")]
        [DefaultValue("")]
        public string BookedByName { get; set; } = string.Empty;

        [Display(Prompt = "Cancelled", Name = "Cancelled")]
        public bool Cancelled { get; set; }

        [Display(Prompt = "Enter full details of this booking", Name = "Details")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2000)]
        [DefaultValue("")]
        public string Details { get; set; } = string.Empty;

        [Display(Prompt = "Customer email", Name = "Email")]
        [DefaultValue("")]
        public string Email { get; set; } = string.Empty;

        [Display(Prompt = "Duration in minutes", Name = "Duration")]
        [DefaultValue(0)]
        public int DurationMinutes { get; set; }

        [Display(Prompt = "Is All Day", Name = "AllDay")]
        public bool IsAllDay { get; set; }

        [Display(Prompt = "Passenger Name", Name = "Passenger Name")]
        [DefaultValue("")]
        public string? PassengerName { get; set; } = string.Empty;

        [Display(Prompt = "Number of Passengers", Name = "Passengers")]
        [DefaultValue(1)]
        public int Passengers { get; set; } = 1;

        [Display(Prompt = "Select payment status", Name = "£ Status", GroupName = "Options")]
        [AllowNull]
        public PaymentStatus? PaymentStatus { get; set; }

        [Display(Prompt = "Is booking confirmed?", Name = "Confirmed", GroupName = "Options")]
        [AllowNull]
        public ConfirmationStatus? ConfirmationStatus { get; set; }

        [Display(Prompt = "Booking scope", Name = "Scope", GroupName = "Options")]
        [AllowNull]
        public BookingScope? Scope { get; set; }

        [Display(Prompt = "Customer telephone", Name = "Phone")]
        [MaxLength(20)]
        [DefaultValue("")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Prompt = "Enter pickup address", Name = "Pickup Address", GroupName = "Pickup Details")]
        [MaxLength(250)]
        [DefaultValue("")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date", GroupName = "Pickup Details")]
        public DateTime PickupDateTime { get; set; }

        public DateTime? ArriveBy { get; set; }

        [Display(Prompt = "Enter pickup postcode", Name = "Pickup Postcode", GroupName = "Pickup Details")]
        [StringLength(9, ErrorMessage = "Pickup postcode should not exceed 9 characters")]
        [MaxLength(9)]
        [DefaultValue("")]
        public string PickupPostCode { get; set; } = string.Empty;

        [Display(Prompt = "Enter destination address", Name = "Destination Address", GroupName = "Destination Details")]
        [MaxLength(250)]
        [DefaultValue("")]
        public string DestinationAddress { get; set; } = string.Empty;

        [Display(Prompt = "Enter destination postcode", Name = "Destination Postcode", GroupName = "Destination Details")]
        [StringLength(9, ErrorMessage = "Destination postcode should not exceed 9 characters")]
        [MaxLength(9)]
        [DefaultValue("")]
        public string DestinationPostCode { get; set; } = string.Empty;

        public List<BookingVia> Vias { get; set; } = new List<BookingVia>();


        public string? RecurrenceException { get; set; }
        public int? RecurrenceID { get; set; }
        public string? RecurrenceRule { get; set; }
        public string UpdatedByName { get; set; } = string.Empty;
        public string CancelledByName { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        public bool ManuallyPriced { get; set; }
        public decimal PriceDiscount { get; set; }
        public decimal PriceAccount { get; set; }
        public decimal? Mileage { get; set; }
        public string? MileageText { get; set; }
        public string? DurationText { get; set; }

        public bool ChargeFromBase { get; set; }
        public int ActionByUserId { get; set; }

        public int? AccountNumber { get; set; }
        public decimal ParkingCharge { get; set; }
        public int WaitingTimeMinutes { get; set; }

        public DateTime? PaymentLinkSentOn { get; set; }
        public string? PaymentLinkSentBy { get; set; }
        public bool IsASAP { get; set; } = false;
    }
}
