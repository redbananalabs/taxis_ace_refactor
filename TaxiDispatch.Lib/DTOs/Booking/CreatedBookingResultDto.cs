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
    public class CreatedBookingResultDto
    {
        public int BookingId { get; set; }
        public DateTime Date { get; set; }
        public string Passenger { get; set; }
        public string BookedBy { get; set; }
        public int AccNo { get; set; }
    }
}
