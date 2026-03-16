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
    public class AccountPassengerDto : ModelValidator
    {
        public int Id { get; set; }

        [Required]
        public int AccNo { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        public string Passenger { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Postcode { get; set; }
        
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Core class which inherits ModelValidator
    /// </summary>
}
