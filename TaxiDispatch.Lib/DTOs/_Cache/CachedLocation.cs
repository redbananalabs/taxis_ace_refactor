
namespace TaxiDispatch.DTOs._Cache
{
    public sealed class CachedLocation
    {
        public int UserId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal Speed { get; set; }
        public decimal Heading { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

