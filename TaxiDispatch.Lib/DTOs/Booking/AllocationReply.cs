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
    public class AllocationReply
    {
        public string Time { get; set; }
        public int JobNo { get; set; }
        public string ColourCode { get; set; }
        public string Driver { get; set; }
        public string Pickup { get; set; }
        public string Passenger{ get; set; }
        public string Response { get; set; }
    }
}
