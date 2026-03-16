using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class DriverOnShift
    {
        [Key]
        public int UserId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? ClearAt { get; set; }
        public bool OnBreak { get; set; }
        public DateTime? BreakStartAt { get; set; }
        public int? ActiveBookingId { get; set; }
        public AppJobStatus Status { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual UserProfile UserProfile { get; set; }

        [ForeignKey(nameof(ActiveBookingId))]
        public virtual Booking Job { get; set; }
    }
}

