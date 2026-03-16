using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.LocalPOI;


namespace TaxiDispatch.DTOs.Booking
{
    public class KeywordSearchResponseDto
    {
        public int? FocusBookingId { get; set; }
        public List<ResultItem> Results { get; set; }

        public class ResultItem
        {
            public int BookingId { get; set; }

            public BookingScope? Scope { get; set; }

            public string CellText
            {
                get
                {
                    if (Scope == BookingScope.Account)
                        return Passenger;
                    else
                        return Pickup + " -- " + Destination;
                }
            }

            public DateTime PickupDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Pickup { get; set; }
            public int DurationMinutes { get; set; }
            public string Destination { get; set; }
            public string Passenger { get; set; }
            public int? UserId { get; set; }
            public string? Color { get; set; }
            public bool Cancelled { get; set; }
        }
    }
}
