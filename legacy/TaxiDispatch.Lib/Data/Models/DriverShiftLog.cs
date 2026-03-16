using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TaxiDispatch.Data.Models
{
    public class DriverShiftLog
    {
        public DriverShiftLog()
        {

        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime? TimeStamp { get; set; }
        public AppDriverShift EntryType { get; set; }
    }
}

