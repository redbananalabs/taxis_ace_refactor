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
    public class EarningsModelDto
    {
        public DateTime Date { get; set; }
        public int? UserId { get; set; }
        public decimal? Price { get; set; }
        public BookingScope? Scope { get; set; }
        public decimal? WaitingPrice { get; set; }
        public decimal? Mileage { get; set; }
    }
}
