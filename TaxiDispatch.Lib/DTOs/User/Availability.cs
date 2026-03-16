using TaxiDispatch.Domain;

namespace TaxiDispatch.DTOs.User
{
    public class Availability
    {
        public Availability()
        {
            AvailableHours = new();
            UnAvailableHours = new();
            AllocatedHours = new();
        }

        public int UserId { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public string ColorCode { get; set; }
        public VehicleType VehicleType { get; set; }
        public List<Hours> AvailableHours { get; set; }
        public List<Hours> UnAvailableHours { get; set; }
        public List<Hours> AllocatedHours { get; set; }
    }
}
