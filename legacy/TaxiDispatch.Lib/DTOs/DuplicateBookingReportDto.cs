using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class DuplicateBookingReportDto
    {
        public string PickupAddress { get; set; }
        public DateTime PickupDateTime { get; set; }
        public string DestinationAddress { get; set; }
        public string? PassengerName { get; set; }
        public int DuplicateCount { get; set; }
    }
}
