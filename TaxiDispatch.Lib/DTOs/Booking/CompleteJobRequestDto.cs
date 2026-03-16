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
    public class CompleteJobRequestDto : ModelValidator
    {
        [Required]
        public int BookingId { get; set; }
        
        [Required]
        public int WaitingTime { get; set; }
        public double ParkingCharge { get; set; }
        [Required]
        public double DriverPrice { get; set; }
        public double AccountPrice { get; set; }
        public double Tip { get; set; }
    }
}
