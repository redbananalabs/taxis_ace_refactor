using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class LocalPOI
    {
        public LocalPOI()
        {
            DateCreated = DateTime.Now.ToUKTime();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Address { get; set; }

        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Area { get; set; }

        [MaxLength(10)]
        public string Postcode { get; set; }

        public LocalPOIType Type { get; set; }

        public double? Longitude { get; set; }
        public double? Latitude { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();
        [SwaggerIgnoreProperty]
        public DateTime? DateUpdated { get; set; }
    }
}

