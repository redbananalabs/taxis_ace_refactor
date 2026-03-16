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
    public class AvailabilityReportDto
    {
        public int UserId { get; set; }
        public List<AvailableHours> AvailableHoursByDay { get; set; } = new();
        public List<UserWeekdayHours> WeekDay { get; set; } = new();
        public List<UserWeekendHours> WeekEnd { get; set; } = new();
        public List<UserWeekHours> Week { get; set; } = new();
        public List<UserMonthHours> Month { get; set; } = new();
        public List<UnAvailableDatesDto> Unavailable { get; set; } = new();
    }
}
