using TaxiDispatch.Domain;

namespace TaxiDispatch.DTOs.Admin
{
    public class DriverOnShiftDto
    {
        public int UserId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? ClearAt { get; set; }
        public bool OnBreak { get; set; }
        public DateTime? BreakStartAt { get; set; }
        public int? ActiveBookingId { get; set; }
        public AppJobStatus Status { get; set; }
        public string ColourCode { get; set; }
        public string Fullname { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleColour { get; set; }
        public string VehicleReg { get; set; }

    }
}

