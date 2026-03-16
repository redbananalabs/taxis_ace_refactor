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
    public class JobsCountModelDto
    {
        public int CashJobsCount { get; set; }
        public int AccJobsCount { get; set; }
        public int RankJobsCount { get; set; }

        public int Total { get { return CashJobsCount + AccJobsCount + RankJobsCount; } }
    }
}
