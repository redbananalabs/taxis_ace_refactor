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
    public class EarningsModelTotalsDto
    {
        public DateTime Date { get; set; }
        public int UserId { get; set; }
        public double CashTotal { get; set; }
        public double AccTotal { get; set; }
        public double RankTotal { get; set; }
        public double CommsTotal { get; set; }
        public double GrossTotal { get; set; }
        public double NetTotal { get; set; }

        public int CashJobsCount { get; set; }
        public int AccJobsCount { get; set; }
        public int RankJobsCount { get; set; }

        
        public decimal? CashMilesCount { get; set; }
        public decimal? AccMilesCount { get; set; }
        public decimal? RankMilesCount { get; set; }

    }
}
