#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class DriverTotalsToday
    {
        public DriverTotalsToday()
        {
            Jobs = new List<JobCompletedDetail>();
        }

        public int TotalJobCount { get { return JourneyJobCount + SchoolRunJobCount; } }
        public int JourneyJobCount { get; set; }
        public int SchoolRunJobCount { get; set; }

        public double EarnedTodayTotal { get; set; }

        public List<JobCompletedDetail> Jobs { get; set; }
    }
}
