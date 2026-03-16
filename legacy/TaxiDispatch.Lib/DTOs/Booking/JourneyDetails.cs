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
    public class JourneyDetails
    {
        public double Miles { get; set; }
        public int Minutes { get; set; }
        public string MileageText { get; set; }
        public string DurationText { get; set; }

        public string StartPostcode { get; set; }
        public string EndPostcode { get; set; }
    }
}
