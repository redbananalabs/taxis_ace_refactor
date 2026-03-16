using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class DriverLocationHistory
    {
        public DriverLocationHistory() 
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        
        [Precision(10, 7)]
        public decimal? Longitude { get; set; }

        [Precision(9, 7)]
        public decimal? Latitude { get; set; }

        [Precision(6, 3)]
        public decimal? Heading { get; set; }

        [Precision(6, 2)]
        public decimal? Speed { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}

