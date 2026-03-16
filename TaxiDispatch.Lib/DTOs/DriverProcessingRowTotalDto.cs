using TaxiDispatch.Domain;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs
{
    public class DriverProcessingRowTotalDto
    {
        public int WaitMinutes { get; set; }
        public double WaitCharge { get; set; }
        public double Parking { get; set; }
        public double Price { get; set; }
        public double Total { get { return WaitCharge + Parking + Price; } }
    }
}
