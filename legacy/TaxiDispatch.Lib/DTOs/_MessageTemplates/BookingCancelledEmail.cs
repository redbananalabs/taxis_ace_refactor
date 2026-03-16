#nullable disable

namespace TaxiDispatch.DTOs.MessageTemplates
{
    public class BookingCancelledEmail
    {
        public int accno { get; set; }
        public string passengername { get; set; }
        public string pickupaddress { get; set; }
        public string destinationaddress { get; set; }
        public string subject { get; set; }
    }
}
