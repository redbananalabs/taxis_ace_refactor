using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.Data.Models
{
    public class AccountTariff
    {
        public AccountTariff() { }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public string Name { get; set; }

        public double AccountInitialCharge { get; set; }
        public double DriverInitialCharge { get; set; }
        
        public double AccountFirstMileCharge { get; set; }
        public double DriverFirstMileCharge { get; set; }

        public double AccountAdditionalMileCharge { get; set; }
        public double DriverAdditionalMileCharge { get; set; }

        [SwaggerIgnoreProperty]
        public DateTime DateCreated { get; set; } = DateTime.Now.ToUKTime();
        [SwaggerIgnoreProperty]
        public DateTime? DateUpdated { get; set; }
    }
}

