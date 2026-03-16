using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class DriverMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();

        [ForeignKey(nameof(UserId))]
        public virtual AppUser User { get; set; }
    }
}

