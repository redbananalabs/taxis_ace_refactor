using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class AccountProcessingRowTotalsDto
    {
        public int WaitMinutes { get; set; }
        public double WaitCharge { get; set; }
        public double Parking { get; set; }
        public double DriverPrice { get; set; }
        public double AccountPrice { get; set; }
        public double Total { get { return WaitCharge + Parking + AccountPrice; } }
    }
}
