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
    public class JourneyItem
    {
        public string JobNo { get; set; }

        public DateTime Date { get; set; }
        public string Passenger { get; set; }
        public string Pickup { get; set; }
        public string Destination { get; set; }
        public string WaitingTime { get; set; }

        public double Waiting { get; set; }
        public double Parking { get; set; }
        public double Journey { get; set; }

        public double Total { get { return Waiting + Parking + Journey; } }
        public double TotalInc { get { return Total + (Waiting + Parking + Journey) * 0.2; } }

        public bool COA { get; set; }
    }
}
