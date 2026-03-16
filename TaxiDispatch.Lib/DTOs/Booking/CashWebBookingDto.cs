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
    public class CashWebBookingDto : WebBookingDto
    {
        public BookingScope? Scope { get; set; }
        public int Passengers { get; set; }
        public int Luggage { get; set; }

        public int DurationMinutes { get; set; }
        public decimal? Mileage { get; set; }
        public string? MileageText { get; set; }
        public string? DurationText { get; set; }
        public double Price { get; set; }

    }
}
