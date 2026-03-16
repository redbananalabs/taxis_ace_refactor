using TaxiDispatch.DTOs.Booking;

namespace TaxiDispatch.DTOs.Admin
{
    public class DashboardDataDto
    {
        public int DriversCount { get; set; } = 0;
        public int BookingsTodayCount { get; set; } = 0;
        public int PoisCount { get; set; } = 0;
        public int UnallocatedTodayCount { get; set; } = 0;

        public List<DriverEarnings> DriverDaysEarnings { get; set; }
        public List<DriverEarnings> DriverWeeksEarnings { get; set; }
        public List<JobsBookedToday> JobsBookedToday { get; set; }
        public int JobsBookedTodayCount { get; set; }
        public List<CustomerCounts> CustomerAquireCounts { get; set; }
        public List<AllocationReply> AllocationReplys { get; set; }
        public List<AllocationStatus> AllocationStatus { get; set; }
        public string LastUpdated { get; set; }

        public DateTime? SmsHeartBeat { get; set; }
    }
}

