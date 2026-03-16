
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class UserActionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public int? BookingId { get; set; }
        public string ActionByUser { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}

