using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class GeoFence
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public List<LatLong> PolygonData { get; set; }
        public int Points { get; set; }
        public string Area { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}

