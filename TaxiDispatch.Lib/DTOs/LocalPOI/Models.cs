using TaxiDispatch.Domain;

namespace TaxiDispatch.DTOs.LocalPOI
{
    public class LocalPOIModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }

        public LocalPOIType Type { get; set; }

        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
    }
}

