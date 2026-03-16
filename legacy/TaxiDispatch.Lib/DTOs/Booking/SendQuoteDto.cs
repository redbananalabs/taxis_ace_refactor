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
    public class SendQuoteDto : ModelValidator
    {
        public string? Subject { get; set; } = "Ace Taxis - Quotation";

        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string Pickup { get; set; }

        public List<string> Vias { get; set; }
        [Required]
        public string Destination { get; set; }
        [Required]
        public string Passenger { get; set; }
        [Required]
        public int Passengers { get; set; }
        [Required]
        public double Price { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime? ReturnTime { get; set; }
        public double? ReturnPrice { get; set; }
    }
}
