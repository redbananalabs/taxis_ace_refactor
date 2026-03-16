#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class BookingAcceptedEmail
    {
        public int accno { get; set; }
        public string passengername { get; set; }
        public string pickupaddress { get; set; }
        public string destinationaddress { get; set; }
        public string subject { get; set; }
        public string datetime { get; set; }
        public int bookingId { get; set; }
        public double price { get; set; }
    }
}
