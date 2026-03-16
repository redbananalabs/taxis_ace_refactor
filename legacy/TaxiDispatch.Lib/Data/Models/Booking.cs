using TaxiDispatch.Domain;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace TaxiDispatch.Data.Models
{
    public class Booking : ICloneable
    {
        public Booking()
        {
            var date = DateTime.Now.ToUKTime();
            VehicleType = VehicleType.Unknown;
            DateCreated = date;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

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

        public DateTime PickupDateTime { get; set; }

        public DateTime? ArriveBy { get; set; }

        public bool IsAllDay { get; set; }

        [DefaultValue(15)]
        public int DurationMinutes { get; set; }

        public string? RecurrenceRule { get; set; } = string.Empty;
        public int? RecurrenceID { get; set; }
        public string? RecurrenceException { get; set; } = string.Empty;

        [MaxLength(250)]
        public string BookedByName { get; set; } = string.Empty;

        public ConfirmationStatus? ConfirmationStatus { get; set; }
        
        public PaymentStatus? PaymentStatus { get; set; }

        public BookingScope? Scope { get; set; }

        public int? AccountNumber { get; set; }
        public int? InvoiceNumber { get; set; }

        [Precision(18,2)]
        public decimal Price { get; set; }

        [Precision(18, 2)]
        public decimal Tip { get; set; }

        public bool ManuallyPriced { get; set; } = false;
        
        [Precision(18, 2)]
        public decimal PriceAccount { get; set; }
        
        [Precision(18, 2)]
        public decimal? Mileage { get; set; }
        public string? MileageText { get; set; }
        public string? DurationText { get; set; }

        [DefaultValue(false)]
        public bool ChargeFromBase { get; set; } = false;

        [DefaultValue(false)]
        public bool Cancelled { get; set; } = false;

        [DefaultValue(false)]
        public bool CancelledOnArrival { get; set; } = false;

        public int? UserId { get; set; }

        public int? SuggestedUserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual UserProfile? User { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime DateCreated { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime? DateUpdated { get; set; }

        [MaxLength(250)]
        public string? UpdatedByName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? CancelledByName { get; set; } = string.Empty;
        
       // [NotMapped]
        public int ActionByUserId { get; set; }

        [SwaggerIgnoreProperty]
        [IgnoreDataMember]
        public virtual List<BookingVia>? Vias { get; set; }

        [ForeignKey(nameof(StatementId))]
        public virtual DriverInvoiceStatement? DriverInvoiceStatement { get; set; }
        
        public int? StatementId { get; set; }

        //[Required]
        public VehicleType VehicleType { get; set; }

        public int WaitingTimeMinutes {  get; set; }
        [Precision(18, 2)]
        public decimal WaitingTimePriceDriver { get; set; }
        [Precision(18, 2)]
        public decimal WaitingTimePriceAccount { get; set; }
        [Precision(18, 2)]
        public decimal ParkingCharge { get; set; }

        public bool PostedForInvoicing { get; set; }

        public bool PostedForStatement { get; set; }

        public DateTime? AllocatedAt { get; set; }

        public int? AllocatedById { get; set; }

        public BookingStatus? Status { get; set; }

        public string? PaymentOrderId { get; set; }

        public string? PaymentLink { get; set; }

        public string? PaymentLinkSentBy { get; set; }
        public DateTime? PaymentLinkSentOn { get; set; }
        public bool PaymentReceiptSent { get; set; }
        
        public bool IsASAP { get; set; }
        [Precision(18, 2)]
        public decimal VatAmountAdded { get; set; }

        [IgnoreDataMember]
        public string CellText
        {
            get 
            {
                var ct = Scope == BookingScope.Account ? PassengerName : $"{PickupAddress} -- {DestinationAddress}";
                return ct;
            }   
        }
        

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}

