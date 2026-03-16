using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class DriverAvailabilityAudit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime ForDate { get; set; }
        [Required]
        public string TheChange { get; set; }
        [Required]
        public string ChangeBy { get; set; }
        [Required]
        public DateTime ChangedOn { get; set; } = DateTime.Now;
    }
}

