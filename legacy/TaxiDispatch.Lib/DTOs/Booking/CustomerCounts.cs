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
    public class CustomerCounts
    {
        public int New { get; set; }
        public int Returning { get; set; }
        public Period PeriodWhen { get; set; }

        public enum Period
        {
            Day,
            Week,
            Month
        }
    }
}
