using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;
using TaxiDispatch.Domain;
using System.Drawing;
using TaxiDispatch.Data.Models;

namespace TaxiDispatch.FormModels
{
    public class BookingFormModel : ModelValidator
    {
        public int? Id { get; set; }

        public string? GoogleEventId { get; set; } = string.Empty;
        public string? GoogleSubject { get; set; } = string.Empty;
        public string? GoogleDescription { get; set; } = string.Empty;
        public string? GoogleLocation { get; set; } = string.Empty;

        [Display(Prompt = "Enter pickup address", Name = "Address", GroupName = "Pickup Details")]
        [MaxLength(250)]
        public string? PickupAddress { get; set; } = string.Empty;

        [Display(Prompt = "Enter pickup postcode", Name = "Post Code", GroupName = "Pickup Details")]
        [StringLength(9, ErrorMessage = "Pickup postcode should not exceed 20 characters")]
        [MaxLength(9)]
        public string PickupPostCode { get; set; } = string.Empty;

        [Display(Prompt = "Enter full details of this booking", Name = "Details")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2000)]
        public string Details { get; set; } = string.Empty;

        [Display(Prompt = "Passenger Name", Name = "Name")]
        public string PassengerName { get; set; } = string.Empty;

        [Display(Prompt = "Customer telephone", Name = "Phone")]
        public string? PhoneNumber { get; set; } = string.Empty;
        
        [Display(Prompt = "Customer email", Name = "Email")]
        public string? Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date", GroupName = "Pickup Details")]
        public DateTime PickupDateTime { get; set; }

        [Display(Name = "EndDateTime")]
        public DateTime EndDateTime { get; set; }

        #region Temp Hidden
        [Display(AutoGenerateField = false)]
        public bool IsAllDay { get; set; }

        [Display(AutoGenerateField = false)]
        public string? RecurrenceRule { get; set; } = string.Empty;
        
        [Display(AutoGenerateField = false)]
        public int? RecurrenceID { get; set; }
        
        [Display(AutoGenerateField = false)]
        public string? RecurrenceException { get; set; } = string.Empty;

        [Display(AutoGenerateField = false)]
        public string BookedByName { get; set; } = string.Empty;

        [Display(AutoGenerateField = false)]
        public int? UserId { get; set; }

        public Color? BackgroundColor { get; set; }
        #endregion

        #region Options - Group 
        [Display(Prompt = "Is booking confirmed?", Name = "Confirmed", GroupName = "Options")]
        [DefaultValue(ConfirmationStatus.NotSet)]
        public ConfirmationStatus ConfirmStatus { get; set; }

        [Display(Prompt = "Select payment type", Name = "Payment", GroupName = "Options")]
        [DefaultValue(PaymentType.NotSet)]
        public PaymentType PayType { get; set; }

        [Display(Prompt = "Select payment status", Name = "$ Status", GroupName = "Options")]
        [DefaultValue(PaymentStatus.NotSet)]
        public PaymentStatus PayStatus { get; set; }

        [Display(Prompt = "Booking scope", Name = "Scope", GroupName = "Options")]
        [DefaultValue(BookingScope.Journey)]
        public BookingScope Scope { get; set; }
        #endregion

    }
}

