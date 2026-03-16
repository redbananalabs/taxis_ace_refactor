using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
          int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
          return dt.AddDays(-1 * diff).Date;
    }

    public static DateTime ToUKTime(this DateTime dt)
        {
            //get the current UTC time
            DateTime localServerTime = dt.ToUniversalTime();

            //Find out if the GMT is in daylight saving time or not.
            var info = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var isDaylightSaving = info.IsDaylightSavingTime(localServerTime);

            //set the time to be the local server time
            var correctDateTime = localServerTime;

            //if the zone is in daylight saving add an hour.
            if (isDaylightSaving)
            {
                correctDateTime = correctDateTime.AddHours(1);
            }

            return correctDateTime;

        }

    public static DateTime To2359(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);
    }

    public static string RemoveExtraSpaces(this string sender)
    {
        if(string.IsNullOrEmpty(sender))
            return sender;

        const RegexOptions options = RegexOptions.None;
        var regex = new Regex("[ ]{2,}", options);
        return regex.Replace(sender, " ").Trim();
    }

    /// <summary>
    /// takes a substring between two anchor strings (or the end of the string if that anchor is null)
    /// </summary>
    /// <param name="this">a string</param>
    /// <param name="from">an optional string to search after</param>
    /// <param name="until">an optional string to search before</param>
    /// <param name="comparison">an optional comparison for the search</param>
    /// <returns>a substring based on the search</returns>
    public static string Substring(this string @this, string from = null, string until = null, StringComparison comparison = StringComparison.InvariantCulture)
    {
        var fromLength = (from ?? string.Empty).Length;
        var startIndex = !string.IsNullOrEmpty(from)
            ? @this.IndexOf(from, comparison) + fromLength
            : 0;

        if (startIndex < fromLength) { throw new ArgumentException("from: Failed to find an instance of the first anchor"); }

        var endIndex = !string.IsNullOrEmpty(until)
        ? @this.IndexOf(until, startIndex, comparison)
        : @this.Length;

        if (endIndex < 0) { throw new ArgumentException("until: Failed to find an instance of the last anchor"); }

        var subString = @this.Substring(startIndex, endIndex - startIndex);
        return subString;
    }
}
