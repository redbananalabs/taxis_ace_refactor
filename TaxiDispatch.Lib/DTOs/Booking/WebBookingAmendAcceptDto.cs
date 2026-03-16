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
    public class WebBookingAmendAcceptDto : ModelValidator
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string ByName { get; set; }

        [Required]
        public DateTime PickupDateTime { get; set; }

        [Required]
        public int Passengers { get; set; }

        [Required]
        public int Vehicles { get; set; } = 1;
    }
}
