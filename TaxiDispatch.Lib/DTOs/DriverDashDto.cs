using TaxiDispatch.DTOs.Booking;

namespace TaxiDispatch.DTOs
{
    public class DriverDashDto
    {
        public int TotalJobCountToday { get; set; }
        public int TotalJobCountWeek { get; set; }

        public double EarningsTotalToday { get; set; }
        public double EarningsTotalWeek { get; set; }
        public double EarningsTotalMonth { get; set; }
        public int TotalJobCountMonth { get; set; }

        //public List<JobCompletedDetail> EarningsToday { get; set; }
        //public List<DriverEarnings> EarningsWeek { get; set; }
        //public List<DriverEarnings> EarningsMonth { get; set; }
    }
}

