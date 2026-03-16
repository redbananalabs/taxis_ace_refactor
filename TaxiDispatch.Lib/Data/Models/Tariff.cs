using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class Tariff
    {
        public Tariff()
        {
            DateCreated = DateTime.Now.ToUKTime();
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public TariffType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double InitialCharge { get; set; }
        public double FirstMileCharge { get; set; }
        public double AdditionalMileCharge { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();
        [SwaggerIgnoreProperty]
        public DateTime? DateUpdated { get; set; }
    }
}

