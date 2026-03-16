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
    public class AirportSearchModel
    {
        public AirportSearchModel()
        {
            LastAirports = new();
        }
        public List<LastTripModel> LastAirports { get; set; }

        public class LastTripModel
        {
            public int UserId { get; set; }
            public string Fullname { get; set; }
            public string Identifier { get { return $"{UserId} - {Fullname}"; } }
            public string Color { get; set; }
            public string Pickup { get; set; }
            public string Destin { get; set; }
            public DateTime Date { get; set; }
            public double Price { get; set; }
        }
    }
}
