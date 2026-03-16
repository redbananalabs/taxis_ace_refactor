using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class UINotification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime DateTimeStamp { get; set; }
        public string Message { get; set; }
        public NotificationEvent Event { get; set; }
        public NotificationStatus Status { get; set; }
    }
}

