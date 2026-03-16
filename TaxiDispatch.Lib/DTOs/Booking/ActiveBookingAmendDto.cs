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
    public class ActiveBookingAmendDto : ActiveBookingDataDto
    {
        public string? Amendments { get; set; }
        public bool CancelBooking { get; set; } = false;
        public DateTime RequestedOn { get; set; }
    }


    /// <summary>
    /// Inherits BookingModel and ModelValidator
    /// </summary>
}
