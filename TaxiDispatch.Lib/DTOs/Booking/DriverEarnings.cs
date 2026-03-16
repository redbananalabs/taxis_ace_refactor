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
    public class DriverEarnings
    {
        public int Id { get; set; }
        public string Fullname { get; set; }

        public double CommsRate { get; set; }

        public string Identifier { get { return $"{Id} - {Fullname}"; } }

        public double Earnings { get { return AccEarned + CashEarned + RankEarned; } }

        /// <summary>
        /// Admin property
        /// </summary>
        public double CommissionCash 
        {
            get
            {
                double total = 0;
                if (CashEarned > 0)
                    total = ((double)CashEarned / 100) * CommsRate;

                return total;
            }
        }

        /// <summary>
        /// Admin property
        /// </summary>
        public double CommissionRank 
        {
            get
            {
                double total = 0;
                if (RankEarned > 0)
                    total = ((double)RankEarned / 100) * CommsRate;

                return total;
            }
        }

        public double Commission 
        { 
            get 
            {
                double total = 0;
                if(CashEarned > 0)
                    total = ((double)CashEarned / 100) * CommsRate; 
                if(RankEarned > 0)
                    total += ((double)RankEarned / 100) * 7.5;

                return total;
            } 
        }

        public int JobsCount { get; set; }
        public string DateRange { get; set; }
        public string ColourCode { get; set; }
        public double RankEarned { get; set; }
        public double CashEarned { get; set; }
        public double AccEarned { get; set; }

        /// <summary>
        /// Driver property
        /// </summary>
        public double TakeHome 
        {
            get
            {
                return (Earnings - Commission);
            }
        }
    }
}
