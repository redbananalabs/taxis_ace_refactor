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
    public class AvailableHours
    {
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public double HoursAvailable { get; set; }

        #region week
        // Record to represent the total hours for a user in a specific week
        public record UserWeekHours(int UserId, int Week, double TotalHours);

        // Main method to calculate total hours per week
        /// <summary>
        /// 
        /// </summary>
        /// <param name="availableHours"></param>
        /// <returns>Key as UserId</returns>
        public List<UserWeekHours> CalculateTotalHoursByWeek(List<AvailableHours> availableHours)
        {
            var groupedByUserAndWeek = GroupByUserAndWeek(availableHours);
            return CalculateTotalHoursForEachUserWeek(groupedByUserAndWeek);
        }

        // Step 1: Group records by UserId and Week of the year using the record
        private IEnumerable<IGrouping<(int UserId, int Week), AvailableHours>> GroupByUserAndWeek(List<AvailableHours> availableHours)
        {
            return availableHours
                .GroupBy(ah => (ah.UserId, GetWeekOfYear(ah.Date)));
        }

        // Step 2: Calculate total hours for each user and week, and return a list of UserWeekHours records
        private List<UserWeekHours> CalculateTotalHoursForEachUserWeek(IEnumerable<IGrouping<(int UserId, int Week), AvailableHours>> groupedByUserAndWeek)
        {
             return groupedByUserAndWeek
               .Select(group => new UserWeekHours(
                group.Key.UserId,                   // UserId from the grouping key
                group.Key.Week,                     // Week number from the grouping key
              group.Sum(ah => ah.HoursAvailable)  // Total hours for this user and week
              ))
             .ToList(); // Create a flat list
        }

        #endregion

        #region month

        // Record to represent the total hours for a user in a specific month
        public record UserMonthHours(int UserId, int Month, double TotalHours);

        // Main method to calculate total hours per month
        /// <summary>
        /// 
        /// </summary>
        /// <param name="availableHours"></param>
        /// <returns>Key as UserId</returns>
        public List<UserMonthHours> CalculateTotalHoursByMonth(List<AvailableHours> availableHours)
        {
            var groupedByUserAndMonth = GroupByUserAndMonth(availableHours);
            return CalculateTotalHoursForEachUserMonth(groupedByUserAndMonth);
        }

        // Step 1: Group records by UserId and Month
        private IEnumerable<IGrouping<(int UserId, int Month), AvailableHours>> GroupByUserAndMonth(List<AvailableHours> availableHours)
        {
            return availableHours
                .GroupBy(ah => (ah.UserId, ah.Date.Month)); // Group by UserId and Month
        }

        // Step 2: Calculate total hours for each user and month, and return a list of UserMonthHours records
        private List<UserMonthHours> CalculateTotalHoursForEachUserMonth(IEnumerable<IGrouping<(int UserId, int Month), AvailableHours>> groupedByUserAndMonth)
        {
               return groupedByUserAndMonth
               .Select(group => new UserMonthHours(
                   group.Key.UserId,                   // UserId from the grouping key
                   group.Key.Month,                    // Month from the grouping key
                   group.Sum(ah => ah.HoursAvailable)  // Total hours for this user and month
               ))
               .ToList(); // Create a flat list
        }

        #endregion month

        #region week day
        // Record to represent the total hours for a user on a specific weekday
        public record UserWeekdayHours(int UserId, DayOfWeek Day, double TotalHours);

        // Main method to calculate total hours per weekday
        public List<UserWeekdayHours> CalculateTotalHoursByWeekday(List<AvailableHours> availableHours)
        {
            var groupedByUserAndDay = GroupByUserAndDay(availableHours);
            return CalculateTotalHoursForEachUserWeekDay(groupedByUserAndDay);
        }

        // Step 1: Group records by UserId and DayOfWeek
        private IEnumerable<IGrouping<(int UserId, DayOfWeek Day), AvailableHours>> GroupByUserAndDay(List<AvailableHours> availableHours)
        {
            return availableHours
                .GroupBy(ah => (ah.UserId, ah.Date.DayOfWeek)); // Group by UserId and DayOfWeek (e.g., Monday)
        }

        // Step 2: Calculate total hours for each user and day and return a list of UserWeekdayHours records
        private List<UserWeekdayHours> CalculateTotalHoursForEachUserWeekDay(IEnumerable<IGrouping<(int UserId, DayOfWeek Day), AvailableHours>> groupedByUserAndDay)
        {
            return groupedByUserAndDay
                .Select(group => new UserWeekdayHours(
                group.Key.UserId,                   // UserId from the grouping key
                group.Key.Day,                      // Day from the grouping key
                group.Sum(ah => ah.HoursAvailable)  // Total hours for this user and day
                ))
                .ToList(); // Create a flat list
        }

        #endregion week day

        #region weekend
        // Record to represent the total hours for a user on a specific weekend day
        public record UserWeekendHours(int UserId, DayOfWeek WeekendDay, double TotalHours);

        // Main method to calculate total hours for weekends
        /// <summary>
        /// 
        /// </summary>
        /// <param name="availableHours"></param>
        /// <returns>Key is UserId</returns>
        public List<UserWeekendHours> CalculateTotalHoursByWeekend(List<AvailableHours> availableHours)
        {
            var groupedByUserAndWeekendDay = GroupByUserAndWeekendDay(availableHours);
            return CalculateTotalHoursForEachUser(groupedByUserAndWeekendDay);
        }

        // Step 1: Group records by UserId and DayOfWeek (for weekends: Saturday and Sunday)
        private IEnumerable<IGrouping<(int UserId, DayOfWeek WeekendDay), AvailableHours>> GroupByUserAndWeekendDay(List<AvailableHours> availableHours)
        {
            return availableHours
                .Where(ah => ah.Date.DayOfWeek == DayOfWeek.Saturday || ah.Date.DayOfWeek == DayOfWeek.Sunday) // Filter weekends
                .GroupBy(ah => (ah.UserId, ah.Date.DayOfWeek)); // Group by UserId and WeekendDay (Saturday/Sunday)
        }

        // Step 2: Calculate total hours for each user and weekend day, and return a list of UserWeekendHours records
        private List<UserWeekendHours> CalculateTotalHoursForEachUser(IEnumerable<IGrouping<(int UserId, DayOfWeek WeekendDay), AvailableHours>> groupedByUserAndWeekendDay)
        {
            return groupedByUserAndWeekendDay
                .Select(group => new UserWeekendHours(
                group.Key.UserId,                   // UserId from the grouping key
                group.Key.WeekendDay,               // WeekendDay (Saturday or Sunday) from the grouping key
                group.Sum(ah => ah.HoursAvailable)  // Total hours for this user and weekend day
            ))
            .ToList(); // Create a flat list
        }
        #endregion

        private int GetWeekOfYear(DateTime date)
        {
            var day = (int)date.DayOfWeek;
            var startOfWeek = date.AddDays(-day + (day == 0 ? -6 : 1)); // Adjust for Sunday to belong to the previous week
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(startOfWeek, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

    }
}
