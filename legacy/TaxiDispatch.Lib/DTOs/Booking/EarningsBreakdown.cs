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
    public class EarningsBreakdown 
    {
        public EarningsBreakdown()
        {
            JobDetails = new List<JobCompletedDetail>();
        }

        public string Date { get; set; }
        public double CommsRate { get; set; }
        public int JobsCount { get; set; }
        public double CashTotal { get; set; }
        public double AccountTotal { get; set; }
        public double RankTotal { get; set; }

        public double EarnedTotal { 
            get 
            {
                return CashTotal + AccountTotal + RankTotal;
            } 
        }

        public double CommisionTotal 
        {
            get
            {
                double total = 0;
                if (CashTotal > 0)
                    total = ((double)CashTotal / 100) * CommsRate;
                if (RankTotal > 0)
                    total += ((double)RankTotal / 100) * 7.5;

                return total;
            }
        }

        public double TakeHome
        {
            get
            {
                return (EarnedTotal - CommisionTotal);
            }
        }

        public List<JobCompletedDetail> JobDetails { get; set; }
    }
}
