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
    public class ChargeableJob 
    {
        public ChargeableJob()
        {
            Vias = new List<BookingVia>();
        }

        public int? AccNo { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public int Passengers { get; set; }
        public string Pickup { get; set; }
        public string PickupPostcode { get; set; }
        public string Destination { get; set; }
        public string DestinationPostcode { get; set; }
        public List<BookingVia>? Vias { get; set; }
        public bool HasVias { 
            get 
            { 
                if(Vias == null)
                    return false;
                else
                    return Vias.Any(); 
            }
        }
        public string? Passenger { get; set; }
        public double Price { get; set; }
        public BookingScope? Scope { get; set; } 
        public bool Cancelled { get; set;}
        public bool COA { get; set; }
        public VehicleType VehicleType { get; set; }
        public double PriceAccount { get; set; }
        public string Details { get; set; }
        public bool HasDetails { get { return !string.IsNullOrEmpty(Details); } }
        public int WaitingMinutes { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public string WaitingTime 
        { 
            get 
            {
                var str = string.Empty;

                if (WaitingMinutes > 0)
                {
                    str = TimeSpan.FromMinutes(WaitingMinutes).ToString(@"hh\:mm");
                }

                return str;
            } 
        }

        public double WaitingPriceDriver
        {
            get
            {
                var permin = 0.32;
                var sum = WaitingMinutes * permin;
                return sum;
            }
        }

        public double WaitingPriceAccount
        {
            get 
            {
                var permin = 0.42;
                var sum = WaitingMinutes * permin;
                return sum;
            }
        }

        public double ParkingCharge { get; set; }

        public double TotalCharge { get { return PriceAccount + WaitingPriceAccount + ParkingCharge; } }

        public double TotalCost { get { return Price + WaitingPriceDriver + ParkingCharge; } }

        public bool PostedForInvoicing { get; set; }

        public bool PostedForStatement { get; set; }

        public double Miles { get; set; }
    }
}
