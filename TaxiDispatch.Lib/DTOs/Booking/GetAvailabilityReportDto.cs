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
    public class GetAvailabilityReportDto : ModelValidator
    {
        public int UserId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }
}
