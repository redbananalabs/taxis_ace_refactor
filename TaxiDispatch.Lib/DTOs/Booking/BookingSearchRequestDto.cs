using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaxiDispatch.DTOs.Booking
{
    public class BookingSearchRequestDto
    {
        public string PickupAddress { get; set; }
        public string PickupPostcode { get; set; }
        public string DestinationAddress { get; set; }
        public string DestinationPostcode { get; set; }
        public string Passenger { get; set; }
        public string PhoneNumber { get; set; }
        public string Details { get; set; }
    }
}
