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
    public class JobsBookedToday
    {
        public string BookedBy { get; set; }
        public int AccountJobs { get; set; }
        public int CashJobs { get; set; }
        public int RankJobs { get; set; }
        public int Total { get { return AccountJobs + CashJobs + RankJobs; } }
    }
}
