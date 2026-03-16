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
    public class DashCounts
    {
        public int UnallocatedTodayCount { get; set; }
        public int BookingsCount { get; set; }
        public int POIsCount { get; set; }
        public int DriversCount { get; set; }
    }
}
