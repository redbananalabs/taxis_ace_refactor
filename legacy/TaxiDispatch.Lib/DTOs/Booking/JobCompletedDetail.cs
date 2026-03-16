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
    public class JobCompletedDetail
    {
        public int BookingId { get; set; }
        public double CommsRate { get; set; }
        public string Pickup { get; set; }
        public string Passenger { get; set; }
        public double Price { get; set; }
        public BookingScope Scope { get; set; }
        public double Commission 
        { 
            get 
            {
                if (Price > 0 && (Scope == BookingScope.Cash || Scope == BookingScope.Card))
                {
                    return ((double)Price / 100) * CommsRate;
                }
                else if (Price > 0 && Scope == BookingScope.Rank)
                {
                    return ((double)Price / 100) * 7.5;
                }

                return 0;
            }
        }
    }
}
