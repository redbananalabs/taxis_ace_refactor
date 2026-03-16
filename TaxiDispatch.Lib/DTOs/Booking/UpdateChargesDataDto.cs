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
    public class UpdateChargesDataDto : ModelValidator
    {
        [Required]
        public int BookingId { get; set; }
        public int WaitingMinutes { get; set; }
        public double ParkingCharge { get; set; }
        public double PriceAccount{ get; set; }
        public double Price { get; set; }
    }
}
