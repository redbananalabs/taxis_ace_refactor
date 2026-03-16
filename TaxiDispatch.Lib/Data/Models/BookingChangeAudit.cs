using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class BookingChangeAudit
    {
        public BookingChangeAudit()
        {
            TimeStamp = DateTime.Now.ToUKTime();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string UserFullName { get; set; }

        public string EntityName { get; set; }

        public string Action { get; set; }

        public string EntityIdentifier { get; set; }

        [MaxLength(50)]
        public string? PropertyName { get; set; }

        [MaxLength(1000)]
        public string? OldValue { get; set; }

        [MaxLength(1000)]
        public string? NewValue { get; set; }

        public DateTime TimeStamp { get; set; }
        
    }
}

