using TaxiDispatch.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiDispatch.DTOs
{
    public class TopCustomerDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? PassengerName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public DateTime? LastBookingDate { get; set; }
    }
}
