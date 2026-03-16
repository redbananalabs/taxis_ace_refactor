using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class WebAmendmentRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public string? Amendments { get; set; }
        public bool CancelBooking { get; set; } = false;
        public bool ApplyToBlock { get; set; } = false;
        public bool Processed { get; set; } = false;
        public DateTime RequestedOn { get; set; }

    }
}

