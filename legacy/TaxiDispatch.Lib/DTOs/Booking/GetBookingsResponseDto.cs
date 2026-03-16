using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.LocalPOI;


namespace TaxiDispatch.DTOs.Booking
{
    public class GetBookingsResponseDto
    {
        public GetBookingsResponseDto()
        {
            Bookings = new List<PersistedBookingModel>();
        }
        public List<PersistedBookingModel> Bookings { get; set; }

        public bool Logout { get; set; }
    }
}
