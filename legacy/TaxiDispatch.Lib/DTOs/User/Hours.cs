namespace TaxiDispatch.DTOs.User
{
    public class Hours
    {
        public TimeSpan? From { get; set; } = new TimeSpan(7, 0, 0);
        public TimeSpan? To { get; set; } = new TimeSpan(17, 0, 0);
        public string Note { get; set; }
    }
}
