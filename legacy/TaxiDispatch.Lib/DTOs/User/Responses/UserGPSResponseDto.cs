namespace TaxiDispatch.DTOs.User.Responses
{
    public class UserGPSResponseDto
    {
        public int UserId { get; set; }
        public string Fullname { get; set; }
        public string RegNo { get; set; }
        public string HtmlColor { get; set; }
        public string Username { get; set; }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public decimal Heading { get; set; }
        public double Speed { get; set; }
        public DateTime GpsLastUpdated { get; set; }
    }
}
