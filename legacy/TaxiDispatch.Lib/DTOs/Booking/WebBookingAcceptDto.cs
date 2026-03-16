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
    public class WebBookingAcceptDto : ModelValidator
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string ByName { get; set; }

        [Required]
        public string RequiredTime { get; set; }

        public double Price { get; set; }

    }
}
