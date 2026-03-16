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
    public class ActiveBookingDataDto
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public DateTime DateTime { get; set; }
        public string PassengerName { get; set; }
        public string PickupAddress { get; set; }
        public string DestinationAddress { get; set; }
        public int? RecurranceId { get; set; }
        public bool ApplyToBlock { get; set; }
        public bool ChangesPending { get; set; }
    }
}
