#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class DriverInvoiceStatementDto
    {
        public int AccountJobsTotalCount { get; set; }
        public int CashJobsTotalCount { get; set; }
        public double CommissionDue { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public double EarningsAccount { get; set; }
        public double EarningsCash { get; set; }
        public double EarningsCard { get; set; }
        public double EarningsRank { get; set; }
        public double CardFees { get; set; }
        public DateTime EndDate { get; set; }
        public bool PaidInFull { get; set; }
        public double PaymentDue { get { return (EarningsAccount + EarningsCard) - CommissionDue; } }
        public int RankJobsTotalCount { get; set; }
        public DateTime StartDate { get; set; }
        public int StatementId { get; set; }
        public double SubTotal { get; set; }
        public double TotalEarned { get { return (EarningsAccount + EarningsCash + EarningsCard + EarningsRank); } }
        public int TotalJobCount { get { return AccountJobsTotalCount + CashJobsTotalCount; } }
        public int UserId { get; set; }
        public string Identifier { get; set; }
        public string ColorCode { get; set; }
        public List<ChargeableJob> Jobs { get; set; } = new List<ChargeableJob>();


    }
}
