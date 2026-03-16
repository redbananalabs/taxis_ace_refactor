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
    public class UnAvailableDatesDto
    {
        public int UserId { get; set; }
        public List<DateTime> UnAvailableDates { get; set; } = new();
        public int TotalUnAvailableDays { get { return UnAvailableDates.Count; } }
    }
}
