using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class DriverAllocation
    {
        [Required]
        public int UserId { get; set; }

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BookingId { get; set; }

        [Required]
        public SentMessageType Type { get; set; }
        [Required]
        public DateTime SentAt { get; set; } = DateTime.Now.ToUKTime();
        [Required]
        public string TwilioResponse { get; set; }

    }
}

