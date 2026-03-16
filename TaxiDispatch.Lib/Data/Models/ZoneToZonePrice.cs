using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class ZoneToZonePrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string StartZoneName { get; set; }
        public string EndZoneName { get; set; }
        [Precision(18,2)]
        public decimal Cost { get; set; }
        [Precision(18, 2)]
        public decimal Charge { get; set; }
        public bool Active { get; set; }
    }
}

