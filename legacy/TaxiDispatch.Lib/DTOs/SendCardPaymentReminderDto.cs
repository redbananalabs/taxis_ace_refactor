using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class SendCardPaymentReminderDto : ModelValidator
    {
        [Required]
        public int BookingId { get; set; }
        [Required]
        public string Phone { get; set; }
    }
}
