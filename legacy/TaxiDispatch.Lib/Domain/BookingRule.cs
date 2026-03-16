using static TaxiDispatch.Domain.BookingRule;

namespace TaxiDispatch.Domain
{
    public class BookingRule
    {
        public enum frequency
        {
            Daily = 0,
            Weekly = 1,
            Fortnightly = 2,
        }
        public BookingRule(string? rule)
        {
            SetRule(rule);
        }

        public frequency Frequency { get; set; } = 0;
        public int Interval { get; set; } = 1;
        public bool Mon { get; set; } = false;
        public bool Tue { get; set; } = false;
        public bool Wed { get; set; } = false;
        public bool Thu { get; set; } = false;
        public bool Fri { get; set; } = false;
        public bool Sat { get; set; } = false;
        public bool Sun { get; set; } = false;

        public DateTime? UntilEnd { set; get; }
        public int CountEnd { set; get; } = 0;

        public List<DateTime> GetDateTimes1(DateTime pickupDateTime)
        {
            List<DateTime> dates = new List<DateTime>();
            if (Frequency == frequency.Daily) // daily
            {
                if (CountEnd > 0)
                {
                    // how many times?
                    for (var i = 0; i < CountEnd; i++)
                    {
                        if (i == 0)
                        {
                            dates.Add(DateTime.Now.AddDays(Interval));
                        }
                        else
                        {
                            var date = dates[i - 1].AddDays(Interval);
                            dates.Add(date);
                        }
                    }
                }
                else // until
                {
                    if (!UntilEnd.HasValue) // never
                    {
                        UntilEnd = pickupDateTime.AddMonths(12);
                    }

                    dates.AddRange(GetAllDatesInclusive(pickupDateTime, UntilEnd.Value));
                }
            }
            else if (Frequency == frequency.Weekly) // weekly
            {
                if (CountEnd > 0)
                {
                    var days = 7 * Interval;

                    // how many times?
                    for (var i = 0; i < CountEnd; i++)
                    {
                        if (i == 0)
                        {
                            dates.Add(pickupDateTime.AddDays(days));
                        }
                        else
                        {
                            var date = dates[i - 1].AddDays(days);
                            dates.Add(date);
                        }
                    }
                }
                else
                {
                    if (!UntilEnd.HasValue)
                    {
                        UntilEnd = pickupDateTime.AddMonths(12);
                    }

                    if (Mon)
                    {
                        var moDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Monday);
                        if (moDates != null)
                            dates.AddRange(moDates);
                    }
                    if (Tue)
                    {
                        var tuDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Tuesday);
                        if (tuDates != null)
                            dates.AddRange(tuDates);
                    }
                    if (Wed)
                    {
                        var weDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Wednesday);
                        if (weDates != null)
                            dates.AddRange(weDates);
                    }
                    if (Thu)
                    {
                        var thDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Thursday);
                        if (thDates != null)
                            dates.AddRange(thDates);
                    }
                    if (Fri)
                    {
                        var frDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Friday);
                        if (frDates != null)
                            dates.AddRange(frDates);
                    }
                    if (Sat)
                    {
                        var saDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Saturday);
                        if (saDates != null)
                            dates.AddRange(saDates);
                    }
                    if (Sun)
                    {
                        var suDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value, DayOfWeek.Sunday);
                        if (suDates != null)
                            dates.AddRange(suDates);
                    }
                }
            }
            else if (Frequency == frequency.Fortnightly)
            { 
            
            }

            // set time
            var newDates = new List<DateTime>();

            foreach (var date in dates)
            {
                var newdate = new DateTime(date.Year, date.Month, date.Day, pickupDateTime.Hour, pickupDateTime.Minute, 0);
                newDates.Add(newdate);
            }

            return newDates;
        }

        // new 

        public List<DateTime> GetDateTimes(DateTime pickupDateTime)
        {
            if (Interval == 2)
            {
                Interval = 1;
                Frequency = frequency.Fortnightly;
            }

            List<DateTime> dates = new List<DateTime>();
            if (Frequency == frequency.Daily || Frequency == frequency.Weekly || Frequency == frequency.Fortnightly)
            {
                int intervalDays = Frequency == frequency.Daily
                ? Interval
                : Frequency == frequency.Weekly
                        ? Interval * 7
                        : Interval * 14;

                if (CountEnd > 0)
                {
                    DateTime nextDate = pickupDateTime;
                    for (int i = 0; i < CountEnd; i++)
                    {
                        nextDate = nextDate.AddDays(intervalDays);
                        dates.Add(SetTime(nextDate, pickupDateTime));
                    }
                }
                else
                {
                    UntilEnd ??= pickupDateTime.AddMonths(6);

                    if (Frequency == frequency.Daily)
                    {
                        DateTime currentDate = pickupDateTime;
                        while (currentDate.Date <= UntilEnd.Value.Date) // Compare only date parts
                        {
                            dates.Add(SetTime(currentDate, pickupDateTime));
                            currentDate = currentDate.AddDays(intervalDays);
                        }
                    }
                    else if (Frequency == frequency.Weekly)
                    {
                        var weekdays = new Dictionary<DayOfWeek, bool>
                        {
                            { DayOfWeek.Monday, Mon },
                            { DayOfWeek.Tuesday, Tue },
                            { DayOfWeek.Wednesday, Wed },
                            { DayOfWeek.Thursday, Thu },
                            { DayOfWeek.Friday, Fri },
                            { DayOfWeek.Saturday, Sat },
                            { DayOfWeek.Sunday, Sun }
                        };

                        foreach (var day in weekdays.Where(d => d.Value))
                        {
                            var weekDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value.Date, day.Key);
                            if (weekDates != null)
                            {
                                dates.AddRange(weekDates
                                   .Where(d => d.Date <= UntilEnd.Value.Date) // Ensure final day is included
                                   .Select(d => SetTime(d, pickupDateTime)));
                            }
                        }
                    }
                    else if (Frequency == frequency.Fortnightly)
                    {
                        var weekdays = new Dictionary<DayOfWeek, bool>
                        {
                            { DayOfWeek.Monday, Mon },
                            { DayOfWeek.Tuesday, Tue },
                            { DayOfWeek.Wednesday, Wed },
                            { DayOfWeek.Thursday, Thu },
                            { DayOfWeek.Friday, Fri },
                            { DayOfWeek.Saturday, Sat },
                            { DayOfWeek.Sunday, Sun }
                        };

                        //// get all dates in range but skip 7 days after each day of week
                        //foreach (var day in weekdays.Where(d => d.Value))
                        //{
                        //    var weekDates = GetWeekdayInRange(pickupDateTime, UntilEnd.Value.Date, day.Key);
                        //    if (weekDates != null)
                        //    {
                        //        dates.AddRange(weekDates
                        //            .Where(d => d.Date <= UntilEnd.Value.Date) // Ensure final day is included
                        //            .Select(d => SetTime(d, pickupDateTime)));
                        //    }
                        //}
                        foreach (var day in weekdays.Where(d => d.Value))
                        {
                            // Find the first occurrence of this weekday on or after pickupDateTime
                            DateTime first = pickupDateTime.Date;
                            while (first.DayOfWeek != day.Key)
                                first = first.AddDays(1);

                            // Add every 14 days until UntilEnd
                            for (DateTime dt = first; dt <= UntilEnd.Value.Date; dt = dt.AddDays(14))
                            {
                                dates.Add(SetTime(dt, pickupDateTime));
                            }
                        }
                    }
                }
            }

            return dates;
        }

        private DateTime SetTime(DateTime date, DateTime reference)
        {
            return new DateTime(date.Year, date.Month, date.Day, reference.Hour, reference.Minute, 0);
        }

        // new


        private List<DateTime> GetAllDatesInclusive(DateTime start, DateTime end)
        {
            List<DateTime> dates = new List<DateTime>();

            end = end.AddDays(1);

            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }

        private void SetRule(string? rule)
        {
            if (!string.IsNullOrEmpty(rule))
            {
                var arr = rule.Split(';');
                foreach (var item in arr)
                {
                    if (item.StartsWith("FREQ"))
                    {
                        var val = item.Split('=')[1];

                        if (val == "DAILY")
                        {
                            Frequency = frequency.Daily;
                        }
                        else if (val == "WEEKLY")
                        {
                            Frequency = frequency.Weekly;
                        }
                    }
                    else if (item.StartsWith("INTERVAL"))
                    {
                        var val = item.Split('=')[1];
                        Interval = Convert.ToInt32(val);
                    }
                    else if (item.StartsWith("COUNT"))
                    {
                        var val = item.Split('=')[1];
                        CountEnd = Convert.ToInt32(val);
                    }
                    else if (item.StartsWith("UNTIL"))
                    {
                        var val = item.Split('=')[1];
                        var year = Convert.ToInt32(val.Substring(0, 4));
                        var month = Convert.ToInt32(val.Substring(4, 2));
                        var day = Convert.ToInt32(val.Substring(6));

                        UntilEnd = new DateTime(year, month, day);
                    }
                    else if (item.StartsWith("BYDAY"))
                    {
                        var val = item.Split('=')[1];
                        var days = val.Split(',');

                        foreach (var d in days)
                        {
                            if (d == "MO")
                                Mon = true;
                            else if (d == "TU")
                                Tue = true;
                            else if (d == "WE")
                                Wed = true;
                            else if (d == "TH")
                                Thu = true;
                            else if (d == "FR")
                                Fri = true;
                            else if (d == "SA")
                                Sat = true;
                            else if (d == "SU")
                                Sun = true;
                        }
                    }
                }
            }
        }

        private static List<DateTime> GetWeekdayInRange(DateTime from, DateTime to, DayOfWeek day)
        {
            Console.WriteLine($"start : {from} - end: {to} DoW: {day}");
            var result = new List<DateTime>();
            if (from <= to)
            {
                for (DateTime dt = from.Date; dt <= to.Date; dt = dt.Date.AddDays(1))
                {
                    if (dt.DayOfWeek == day)
                    {
                        result.Add(dt);
                        Console.WriteLine(dt);
                    }
                }
            }

            return result;
        }
    }
}

