using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiDispatch.Data.Models
{
    public class FixedPriceJourney
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string PassengerName { get; set; }
        public string PickupPostcode { get; set; }
        public string DestinationPostcode { get; set; }
        public double DriverPrice { get; set; }
        public double AccountPrice { get; set; }
        public double JourneyMileage { get; set; }
        public double DeadMileage { get; set; }
        public int JourneyMinutes { get; set; }
        public int DeadMinutes { get; set; }

    }
}

