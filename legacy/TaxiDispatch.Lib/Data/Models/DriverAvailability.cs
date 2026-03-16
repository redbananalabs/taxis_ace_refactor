using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TaxiDispatch.Domain;

namespace TaxiDispatch.Data.Models
{
    public class DriverAvailability
    {
        public DriverAvailability()
        {
            Description = string.Empty;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Description { get; set; }

        public int UserId { get; set; }
        
        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DriverAvailabilityType AvailabilityType { get; set; }
        public bool GiveOrTake { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser User { get; set; }
    }
}

