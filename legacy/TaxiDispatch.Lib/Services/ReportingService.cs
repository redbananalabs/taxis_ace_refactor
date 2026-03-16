using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace TaxiDispatch.Services
{
    public class ReportingService : BaseService<ReportingService>
    {
        public ReportingService(
            IDbContextFactory<TaxiDispatchContext> factory,ILogger<ReportingService> logger) 
            : base(factory,logger)
        {

        }


        #region ADMIN FUNCTIONS
        public async Task<List<DriverEarnings>> GetDriversTotals(DateTime from, DateTime to, int userId)
        {
            var ukfrom = from.ToUKTime();
            var ukto = to.ToUKTime().To2359();

            var userRole = await _dB.UserRoles
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .FirstOrDefaultAsync();

            var data = await _dB.Bookings
                .AsNoTracking()
                .Where(o => o.PickupDateTime.Date >= ukfrom && o.PickupDateTime <= ukto && (o.Cancelled == false))
                .ToListAsync();

            // remove any jobs that are account and later that 17:00 and less than 06:00 and not on weekends
            if (userId != 1)
            { 
                data.RemoveAll(o => o.Scope == BookingScope.Account && 
                    o.PickupDateTime.Hour > 17 || o.PickupDateTime.Hour < 6);

                data.RemoveAll(o => o.Scope == BookingScope.Account &&
                    o.PickupDateTime.DayOfWeek == DayOfWeek.Saturday || o.PickupDateTime.DayOfWeek == DayOfWeek.Sunday);
            }

            var dgrp = data.GroupBy(o => o.UserId);
            var lst = new List<DriverEarnings>();

            var rangeText = string.Empty;
            if (from != to)
            {
                rangeText = "Date Range";
            }
            else
            {
                rangeText = "Day";
            }

            foreach (var grp in dgrp)
            {
                if (userRole.RoleId != 1)
                {
                    if(grp.Key == 1)
                    {
                        continue;
                    }
                }

                var profile = await _dB.UserProfiles
                    .AsNoTracking()
                    .Include(o => o.User)
                    .Where(o => o.UserId == grp.Key)
                    .FirstOrDefaultAsync();

                if (profile != null) // unallocated
                {
                    var total = grp.Sum(o => o.Price);
                    
                    var totCash = grp.Where(o=>o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Sum(o => o.Price);
                    var totAcc = grp.Where(o => o.Scope == BookingScope.Account).Sum(o => o.Price);
                    var totRank = grp.Where(o => o.Scope == BookingScope.Rank).Sum(o => o.Price);

                    var cnt = grp.Count();

                    var obj = new DriverEarnings
                    {
                        Id = profile.UserId,
                        Fullname = $"{profile.User.FullName}",
                        DateRange = rangeText,
                        CashEarned = userRole.RoleId == 1 ? (double)totCash : 0,
                        AccEarned = (double)totAcc,
                        RankEarned = userRole.RoleId == 1 ? (double)totRank : 0,
                        JobsCount = cnt,
                        ColourCode = profile.ColorCodeRGB,
                        CommsRate = profile.CashCommissionRate
                    };

                    lst.Add(obj);
                }
            }

            return lst.OrderBy(o=>o.Earnings).ToList();
        }

        public async Task<List<JobsBookedToday>> JobsBookedToday()
        {
            var from = DateTime.Now.ToUKTime().Date;
            var to = DateTime.Now.ToUKTime().To2359();

            var jobs = await _dB.Bookings.Where(o => (o.DateCreated.Date == from) &&
            (o.DateCreated.Date <= to) && o.Cancelled == false)
                .Select(o => new { Scope = o.Scope, BookedBy = o.BookedByName }).ToListAsync();

            var list = new List<JobsBookedToday>();

            var grp = jobs.GroupBy(o => o.BookedBy).ToList();

            foreach (var item in grp)
            {
                var obj = new JobsBookedToday();
                obj.BookedBy = item.Key;
                obj.AccountJobs = item.Count(o => o.Scope == BookingScope.Account);
                obj.RankJobs = item.Count(o => o.Scope == BookingScope.Rank);
                obj.CashJobs = item.Count(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card);

                list.Add(obj);
            }

            return list;
        }

        public async Task<DashCounts> GetDashCounts()
        {
            //var data = await _dB.Bookings.Where(o => o.PickupDateTime.Date == DateTime.Now.Date && o.Cancelled == false).ToListAsync();

            var obj = new DashCounts
            {
                POIsCount = await _dB.LocalPOIs.CountAsync(),
                DriversCount = await _dB.UserProfiles.AsNoTracking().Include(o=>o.User)
                    .Where(o => !o.IsDeleted && o.User.LockoutEnabled == false).CountAsync(),

                BookingsCount = await _dB.Bookings.Where(o => o.PickupDateTime.Date == DateTime.Now.Date &&
                    (o.Cancelled == false)).CountAsync(),
                
                UnallocatedTodayCount = await _dB.Bookings
                    .Where(o => o.PickupDateTime.Date == DateTime.Now.Date &&
                        o.Cancelled == false &&
                        o.UserId == null).CountAsync()
            };

            return obj;
        }

        public async Task<List<CustomerCounts>> GetCustomerCounts()
        {
            var list = new List<CustomerCounts>();

            var monthStart = DateTime.Now.Date.AddMonths(-1);
            var weekStart = DateTime.Now.Date.AddDays(-7);
            var end = DateTime.Now.Date;

            var data = await _dB.Bookings.Where(o => o.PickupDateTime.Date >= monthStart && 
                ((o.Cancelled == false)) &&
                o.PickupDateTime.Date <= end.Date.To2359())
                .AsNoTracking().Select(o => new {
                    PhoneNumber = o.PhoneNumber,
                    PickupDate = o.PickupDateTime
                }
                ).ToListAsync();


            var weekBookings = data.Where(o => o.PickupDate.Date >= weekStart.Date && o.PickupDate.Date <= end.Date.To2359());
            var todaysBookings = data.Where(o => o.PickupDate.Date == end.Date);

            var grpMonth = data.GroupBy(o => o.PhoneNumber);
            var grpWeek = weekBookings.GroupBy(o => o.PhoneNumber);
            var grpToday = todaysBookings.GroupBy(o => o.PhoneNumber);

            var objMonth = new CustomerCounts()
            {
                New = grpMonth.Count(),
                Returning = grpMonth.Where(o => o.Count() > 1).Count(),
                PeriodWhen = CustomerCounts.Period.Month
            };

            var objWeek = new CustomerCounts()
            {
                New = grpWeek.Count(),
                Returning = grpWeek.Where(o => o.Count() > 1).Count(),
                PeriodWhen = CustomerCounts.Period.Week
            };

            var objDay = new CustomerCounts()
            {
                New = grpToday.Count(),
                Returning = grpToday.Where(o => o.Count() > 1).Count(),
                PeriodWhen = CustomerCounts.Period.Day
            };

            list.Add(objMonth);
            list.Add(objWeek);
            list.Add(objDay);

            return list;
        }

        public async Task<List<AllocationStatus>> GetAllocationStatus()
        { 
            var lst = new List<AllocationStatus>();

            // get last 5
            var data = await _dB.DriverAllocations
                .AsNoTracking()
                .OrderByDescending(o => o.SentAt)
                .Take(5)
                .ToListAsync();

            if (data != null)
            {
                foreach (var item in data)
                {
                    var obj = new AllocationStatus();

                    // get driver 
                    var profile = await _dB.UserProfiles.Where(o => o.UserId == item.UserId)
                        .Include(o => o.User).AsNoTracking().FirstOrDefaultAsync();

                    if (profile == null)
                        continue;

                    // get passenger and pickup
                    var jobData = await _dB.Bookings.Where(o => o.Id == item.BookingId)
                        .Select(o => new { Pickup = o.PickupAddress, Passenger = o.PassengerName }).FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(profile.User.FullName) && jobData != null)
                    {
                        obj.Passenger = jobData.Passenger;
                        obj.Pickup = jobData.Pickup;
                        obj.JobNo = item.BookingId;
                        obj.Response = item.TwilioResponse;
                        obj.ColourCode = profile.ColorCodeRGB;
                        obj.Driver = $"{item.UserId} - {profile.User.FullName}";
                        obj.Time = item.SentAt.ToString("HH:mm:ss");

                        lst.Add(obj);
                    }
                }
            }

            return lst;
        }

        public async Task<List<AllocationReply>> GetAllocationReplys()
        { 
            var lst = new List<AllocationReply>();

            // get last 5
            var data = await _dB.DriverMessages
                .AsNoTracking()
                .OrderByDescending(o=>o.DateCreated)
                .Take(5)
                .ToListAsync();

            if (data != null)
            {  
                foreach (var item in data) 
                {
                    var obj = new AllocationReply();
                    var arr = item.Message.Split("-");

                    var response = arr[0].Trim();
                    var jid = arr[1].Trim();

                    if (string.IsNullOrEmpty(jid))
                        continue;

                    var bookingId = Convert.ToInt32(jid);

                    // get driver 
                    var profile = await _dB.UserProfiles.Where(o => o.UserId == item.UserId)
                        .Include(o => o.User).AsNoTracking().FirstOrDefaultAsync();
                    
                    // get passenger and pickup
                    var jobData = await _dB.Bookings.Where(o => o.Id == bookingId)
                        .Select(o => new { Pickup = o.PickupAddress, Passenger = o.PassengerName }).FirstOrDefaultAsync();

                    try
                    {
                        if (!string.IsNullOrEmpty(profile.User.FullName) && jobData != null)
                        {
                            obj.Passenger = jobData.Passenger;
                            obj.Pickup = jobData.Pickup;
                            obj.JobNo = bookingId;
                            obj.Response = response;
                            obj.ColourCode = profile.ColorCodeRGB;
                            obj.Driver = $"{item.UserId.Value} - {profile.User.FullName}";
                            obj.Time = item.DateCreated.ToString("HH:mm:ss");

                            lst.Add(obj);
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex.Message,ex);
                    }
                }
            }
            return lst;
        }

        public async Task<DateTime?> GetSmsHeartBeat()
        {
            var data = await _dB.MessagingNotifyConfig.AsNoTracking().FirstOrDefaultAsync();
            if (data != null) 
            {
                return data.SmsPhoneHeartBeat;
            }

            return null;
        }

        #endregion

        #region DRIVER FUNCTIONS

        public async Task DashTotals(int userid)
        {
            await _dB.Bookings.Where(o => o.Cancelled == false &&
                (o.PickupDateTime.Date >= DateTime.Now.Date) &&
                o.UserId == userid)
                .Select(o => new { o.Price, o.WaitingTimePriceDriver })
                .ToListAsync();
        }

        public async Task<DriverTotalsToday> DriverLoadTodaysTotals(int userId)
        {
            var obj = new DriverTotalsToday();

            var now = DateTime.Now.Date.ToUKTime();

            var journeysToday = await _dB.Bookings.CountAsync(o => o.UserId == userId && (o.Cancelled == false) && 
                o.PickupDateTime.Date == now.Date && (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card || o.Scope == BookingScope.Rank));

            var accsToday = await _dB.Bookings.CountAsync(o => o.UserId == userId && (o.Cancelled == false) &&
                o.PickupDateTime.Date == now.Date && o.Scope == BookingScope.Account);

            var earnToday = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) &&
                o.PickupDateTime.Date == now.Date).Select(o => o.Price).SumAsync();

            var jobsDoneToday = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false)  &&
                o.PickupDateTime.Date == now.Date).Select(o => new
                {
                    BookingId = o.Id,
                    Pickup = o.PickupAddress,
                    Passenger = o.PassengerName,
                    Price = o.Price,
                    Scope = o.Scope
                }).ToListAsync();

            var comms = await _dB.UserProfiles.Where(o => o.UserId == userId).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();

            obj.JourneyJobCount = journeysToday;
            obj.SchoolRunJobCount = accsToday;
            obj.EarnedTodayTotal = (double)earnToday;

            foreach (var item in jobsDoneToday)
            {
                obj.Jobs.Add(new JobCompletedDetail
                {
                    BookingId = item.BookingId,
                    Pickup = item.Pickup,
                    Passenger = item.Passenger,
                    Price = (double)item.Price,
                    Scope = (BookingScope)item.Scope,
                    CommsRate = comms
            });
            }

            return obj;
        }

        public async Task<DriverEarnings> DriverLoadMonthsTotals(int userId)
        {
            var obj = new DriverEarnings();
            var to = DateTime.Now.To2359();
            // day 1 of current month
            var monthStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).Date;

            var monthJobsCount = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) &&
                o.PickupDateTime.Date >= monthStartDate && o.PickupDateTime.Date <= to)
                    .CountAsync();

            var monthJobsCashSum = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) && 
            (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card) &&
                o.PickupDateTime.Date >= monthStartDate && o.PickupDateTime.Date <= to)
                .Select(o => o.Price)
                .SumAsync();

            var monthJobsAccSum = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) && 
            (o.Scope == BookingScope.Account || o.Scope == BookingScope.Rank) &&
                o.PickupDateTime.Date >= monthStartDate && o.PickupDateTime.Date <= to)
                .Select(o => o.Price)
                .SumAsync();

            var comms = await _dB.UserProfiles.Where(o => o.UserId == userId).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();

            obj.CommsRate = comms;
            obj.JobsCount = monthJobsCount;
            obj.CashEarned = (double)monthJobsCashSum;
            obj.AccEarned = (double)monthJobsAccSum;
            obj.DateRange = monthStartDate.ToString("dd MMM") + " - " + DateTime.Now.ToString("dd MMM");

            return obj;
        }

        public async Task<DriverEarnings> DriverLoadWeeksTotals(int userId)
        {
            var obj = new DriverEarnings();

            var from = DateTime.Now.StartOfWeek(DayOfWeek.Monday);
            var to = DateTime.Now.To2359();

            var weekJobsCount = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) &&
                o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to)
                    .CountAsync();

                var weekJobsCashSum = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) && 
                (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card) &&
                o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to)
                .Select(o => o.Price)
                .SumAsync();

            var weekJobsAccSum = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) && 
            (o.Scope == BookingScope.Account) &&
                o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to)
                .Select(o => o.Price)
                .SumAsync();

            var weekRankSum = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) &&
                (o.Scope == BookingScope.Rank) &&
                o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to)
                .Select(o => o.Price)
                .SumAsync();

            var comms = await _dB.UserProfiles.Where(o => o.UserId == userId).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();

            obj.CommsRate = comms;
            obj.JobsCount = weekJobsCount;
            obj.CashEarned = (double)weekJobsCashSum;
            obj.AccEarned = (double)weekJobsAccSum;
            obj.RankEarned = (double)weekRankSum;
            obj.DateRange = from.ToString("dd MMM") + " - " + to.ToString("dd MMM");

            return obj;
        }

        public async Task<DriverTotalsForDateRange> LoadTotalsForDateRange(int userId, DateTime from, DateTime to)
        {
            var res = new DriverTotalsForDateRange();

            var lst = new List<EarningsBreakdown>();

            var jobs = await _dB.Bookings.Where(o => o.UserId == userId && (o.Cancelled == false) &&
               o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to.Date.To2359() && o.Price != null)
                .Select(o => new
                {
                    Date = o.PickupDateTime,
                    BookingId = o.Id,
                    Pickup = o.PickupAddress,
                    Passenger = o.PassengerName,
                    Price = o.Price,
                    Scope = o.Scope
                })
                .ToListAsync();

            // group by date
            var jobsByDay = jobs.GroupBy(o => o.Date.Date).ToList();

            var comms = await _dB.UserProfiles.Where(o => o.UserId == userId).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();

            foreach (var day in jobsByDay)
            {
                var obj = new EarningsBreakdown();
                obj.CommsRate = comms;

                obj.Date = day.Key.ToString("dd MMM");
                obj.JobsCount = day.Count();

                var cash = day
                    .Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card)
                    .Select(o => o.Price)
                    .ToList();
                obj.CashTotal = cash != null ? (double)cash.Sum() : 0.0;

                var acc = day
                    .Where(o => o.Scope == BookingScope.Account)
                    .Select(o => o.Price)
                    .ToList();
                obj.AccountTotal = acc != null ? (double)acc.Sum() : 0.0;

                var rank = day
                    .Where(o => o.Scope == BookingScope.Rank)
                    .Select(o => o.Price)
                    .ToList();
                obj.RankTotal = rank != null ? (double)rank.Sum() : 0.0;

         
                foreach (var job in day)
                {
                    var price = (double)job.Price > 0 ? (double)job.Price : 0;

                    obj.JobDetails.Add(new JobCompletedDetail
                    {
                        BookingId = job.BookingId,
                        Passenger = job.Passenger,
                        Pickup = job.Pickup,
                        Price = price,
                        Scope = (BookingScope)job.Scope,
                        CommsRate = comms
                    });
                }

                lst.Add(obj);
            }

            res.Earnings = lst;

            var cashs = jobs.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Count();
            var accs = jobs.Where(o => o.Scope == BookingScope.Account).Count();
            var ranks = jobs.Where(o => o.Scope == BookingScope.Rank).Count();

            res.AccountJobs = accs;
            res.CashJobs = cashs;
            res.RankJobs = ranks;

            return res;
        }

        public async Task<List<ChargeableJob>> GetDriversJobs(int userId, DateTime from, DateTime to)
        {
            var res = new List<ChargeableJob>();

            var jobs = await _dB.Bookings
                .Where(o => (o.UserId == userId)
                    && (o.Cancelled == false) 
                    && ((o.PickupDateTime >= from) && (o.PickupDateTime <= to.To2359())))
                .AsNoTracking()
                .Select(o => new ChargeableJob
                {
                    BookingId = o.Id,
                    Date = o.PickupDateTime,
                    Pickup = $"{o.PickupAddress}, {o.PickupPostCode}",
                    PickupPostcode = o.PickupPostCode,
                    Destination = $"{o.DestinationAddress}, {o.DestinationPostCode}",
                    DestinationPostcode = o.DestinationPostCode,
                    Passenger = o.PassengerName,
                    Passengers = o.Passengers,
                    Vias = o.Vias.Any() ? o.Vias : new List<BookingVia>(),
                    Price = (double)o.Price,
                    Scope = (BookingScope)o.Scope,
                    UserId = o.UserId.Value,
                    Cancelled = o.Cancelled,
                    COA = o.CancelledOnArrival
                })
                .ToListAsync();

            res.AddRange(jobs);

            return res;
        }

        #endregion

        #region REPORTS

        public async Task<List<DuplicateBookingReportDto>> DuplicateBookingsReport(DateTime from)
        {
            var duplicates = await _dB.Bookings
            .Where(b => !b.Cancelled && b.PickupDateTime >= from)
            .GroupBy(b => new
            {
                b.PickupAddress,
                b.PickupDateTime,
                b.DestinationAddress,
                b.PassengerName
            })
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateBookingReportDto
            {
                PickupAddress = g.Key.PickupAddress,
                PickupDateTime = g.Key.PickupDateTime,
                DestinationAddress = g.Key.DestinationAddress,
                PassengerName = g.Key.PassengerName,
                DuplicateCount = g.Count()
            })
            .ToListAsync();

            return duplicates;
        }

        // BY SCOPE
        public async Task<List<ScopeBreakdownEntry>> GetBookingScopeBreakdownAsync(DateTime from,DateTime to,ViewPeriodBy period,
            bool compareWithPreviousYear)
        {
                    var allPeriods = new List<ScopeBreakdownEntry>();

                    var baseQuery = _dB.Bookings
                        .Where(b => !b.Cancelled && b.PickupDateTime >= from && b.PickupDateTime <= to);

                    allPeriods.AddRange(await GroupByPeriod(baseQuery, period, isComparison: false));

                    if (compareWithPreviousYear)
                    {
                        var prevFrom = from.AddYears(-1);
                        var prevTo = to.AddYears(-1);
                        var prevQuery = _dB.Bookings
                            .Where(b => !b.Cancelled && b.PickupDateTime >= prevFrom && b.PickupDateTime <= prevTo);

                        allPeriods.AddRange(await GroupByPeriod(prevQuery, period, isComparison: true));
                    }

                    return allPeriods;
        }

        private async Task<List<ScopeBreakdownEntry>> GroupByPeriod(IQueryable<Booking> query,ViewPeriodBy period, bool isComparison)
        {
            switch (period)
            {
                case ViewPeriodBy.Daily:
                    return await query
                        .GroupBy(b => new
                        {
                            Date = b.PickupDateTime.Date,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = g.Key.Date.ToString("yyyy-MM-dd"),
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            ScopeText = (g.Key.Scope ?? BookingScope.Cash).ToString(),
                            Count = g.Count(),
                            IsComparison = isComparison
                        })
                        .ToListAsync();

                case ViewPeriodBy.Monthly:
                    return await query
                        .GroupBy(b => new
                        {
                            b.PickupDateTime.Year,
                            b.PickupDateTime.Month,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = $"{g.Key.Year}-{g.Key.Month:D2}",
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            Count = g.Count(),
                            IsComparison = isComparison
                        })
                        .ToListAsync();

                case ViewPeriodBy.Weekly:
                    return query
                        .AsEnumerable() // switch to client-side for week calculation
                        .GroupBy(b => new
                        {
                            Week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                                b.PickupDateTime,
                                CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday),
                            b.PickupDateTime.Year,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = $"Week {g.Key.Week}-{g.Key.Year}",
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            ScopeText = (g.Key.Scope ?? BookingScope.Cash).ToString(),
                            Count = g.Count(),
                            IsComparison = isComparison
                        }).ToList();

                case ViewPeriodBy.Quaterly:
                    return await query
                        .GroupBy(b => new
                        {
                            Quarter = ((b.PickupDateTime.Month - 1) / 3) + 1,
                            b.PickupDateTime.Year,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = $"Q{g.Key.Quarter}-{g.Key.Year}",
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            ScopeText = (g.Key.Scope ?? BookingScope.Cash).ToString(),
                            Count = g.Count(),
                            IsComparison = isComparison
                        })
                        .ToListAsync();

                case ViewPeriodBy.Yearly:
                    return await query
                        .GroupBy(b => new
                        {
                            b.PickupDateTime.Year,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = g.Key.Year.ToString(),
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            ScopeText = (g.Key.Scope ?? BookingScope.Cash).ToString(),
                            Count = g.Count(),
                            IsComparison = isComparison
                        })
                        .ToListAsync();

                case ViewPeriodBy.Hourly:
                default:
                    return await query
                        .GroupBy(b => new
                        {
                            b.PickupDateTime.Year,
                            b.PickupDateTime.Month,
                            b.PickupDateTime.Day,
                            b.PickupDateTime.Hour,
                            b.Scope
                        })
                        .Select(g => new ScopeBreakdownEntry
                        {
                            PeriodLabel = $"{g.Key.Year}-{g.Key.Month:D2}-{g.Key.Day:D2} {g.Key.Hour:D2}:00",
                            Scope = g.Key.Scope ?? BookingScope.Cash,
                            ScopeText = (g.Key.Scope ?? BookingScope.Cash).ToString(),
                            Count = g.Count(),
                            IsComparison = isComparison
                        })
                        .ToListAsync();
            }
        }
        // BY SCOPE

        // TOP CUSTOMER
        public async Task<List<TopCustomerDto>> GetTopCustomers(DateTime? from = null, DateTime? to = null, BookingScope? scope = null, int topN = 10)
        {
            var query = _dB.Bookings
                .Where(b => !string.IsNullOrEmpty(b.PhoneNumber) && !b.Cancelled);

            if (from.HasValue)
                query = query.Where(b => b.PickupDateTime >= from.Value);

            if (to.HasValue)
                query = query.Where(b => b.PickupDateTime <= to.Value);

            if (scope.HasValue)
            {
                if (scope != BookingScope.All)
                    query = query.Where(b => b.Scope == scope);
            }

            var result = await query
                .GroupBy(b => b.PhoneNumber)
                .Select(g => new TopCustomerDto
                {
                    PhoneNumber = g.Key,
                    PassengerName = g.OrderByDescending(b => b.PickupDateTime).Select(b => b.PassengerName).FirstOrDefault(),
                    BookingCount = g.Count(),
                    LastBookingDate = g.Max(b => b.PickupDateTime)
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(topN)
                .ToListAsync();

            return result;
        }
        // TOP CUSTOMER

        // BY POSTCODE
        public async Task<List<PickupPostcodeDto>> GetPickupPostcodes(DateTime? from = null, DateTime? to = null, BookingScope? scope = null)
        {
            var query = _dB.Bookings
                .Where(b => !string.IsNullOrEmpty(b.PickupPostCode) && !b.Cancelled);

            if (from.HasValue)
                query = query.Where(b => b.PickupDateTime >= from.Value);

            if (to.HasValue)
                query = query.Where(b => b.PickupDateTime <= to.Value);

            if (scope.HasValue)
            {
                if (scope != BookingScope.All)
                    query = query.Where(b => b.Scope == scope);
            }

            var result = await query
                .GroupBy(b => b.PickupPostCode.Trim().ToUpper())
                .Select(g => new PickupPostcodeDto
                {
                    PickupPostCode = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return result;
        }
        // BY POSTCODE

        // BY VEHICLE TYPE
        public async Task<List<VehicleTypeCountDto>> GetVehicleTypeCounts(DateTime? from = null, DateTime? to = null, BookingScope? scope = null)
        {
            var query = _dB.Bookings
                .Where(b => !b.Cancelled); // Exclude cancelled by default

            if (from.HasValue)
                query = query.Where(b => b.PickupDateTime >= from.Value);

            if (to.HasValue)
                query = query.Where(b => b.PickupDateTime <= to.Value);

            if (scope.HasValue)
            {
                if (scope != BookingScope.All)
                    query = query.Where(b => b.Scope == scope);
            }

            var result = await query
                .GroupBy(b => b.VehicleType)
                .Select(g => new VehicleTypeCountDto
                {
                    VehicleType = g.Key,
                    VehicleTypeText = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return result;
        }
        // BY VEHICLE TYPE

        // AVERAGE DURATION
        public async Task<List<PeriodAverageDurationDto>> GetAverageDuration(DateTime from, DateTime to,ViewPeriodBy period, BookingScope? scope = null)
        {
            var query = _dB.Bookings
                .Where(b => !b.Cancelled && b.DurationMinutes > 0 &&
                            b.PickupDateTime >= from && b.PickupDateTime <= to);

            if (scope.HasValue)
            {
                if(scope != BookingScope.All)
                    query = query.Where(b => b.Scope == scope); 
            }

            var data = await query
                .Select(b => new
                {
                    b.PickupDateTime,
                    b.DurationMinutes
                })
                .ToListAsync();

            var grouped = data.GroupBy(x =>
            {
                return period switch
                {
                    ViewPeriodBy.Daily => x.PickupDateTime.ToString("yyyy-MM-dd"),
                    ViewPeriodBy.Weekly => ISOWeek.GetYear(x.PickupDateTime) + "-W" + ISOWeek.GetWeekOfYear(x.PickupDateTime),
                    ViewPeriodBy.Monthly => x.PickupDateTime.ToString("yyyy-MM"),
                    ViewPeriodBy.Yearly => x.PickupDateTime.Year.ToString(),
                    _ => x.PickupDateTime.ToString("yyyy-MM-dd")
                };
            });

            var result = grouped.Select(g => new PeriodAverageDurationDto
            {
                PeriodLabel = g.Key,
                AverageDurationMinutes = Math.Round(g.Average(x => x.DurationMinutes), 2),
                TotalBookings = g.Count()
            })
            .OrderBy(x => x.PeriodLabel)
            .ToList();

            return result;
        }
        // AVERAGE DURATION

        // GROWTH 
        public async Task<List<ScopeTimeGrowthDto>> GetBookingGrowthByMonthOrYear(
        int startMonth, int startYear,
        int endMonth, int endYear,
        GroupByType groupBy)
        {
            var currentFrom = new DateTime(startYear, startMonth, 1);
            var currentTo = new DateTime(endYear, endMonth, DateTime.DaysInMonth(endYear, endMonth));
            var previousFrom = currentFrom.AddYears(-1);
            var previousTo = currentTo.AddYears(-1);

            // Pull all relevant bookings
            var bookings = await _dB.Bookings
                .Where(b => !b.Cancelled &&
                            b.Scope != null &&
                            (
                                (b.PickupDateTime >= currentFrom && b.PickupDateTime <= currentTo) ||
                                (b.PickupDateTime >= previousFrom && b.PickupDateTime <= previousTo)
                            ))
                .Select(b => new
                {
                    b.PickupDateTime,
                    Scope = b.Scope!.Value
                })
                .ToListAsync();

            var transformed = bookings.Select(b =>
            {
                var year = b.PickupDateTime.Year;
                var periodKey = groupBy switch
                {
                    GroupByType.Month => b.PickupDateTime.Month, // 1-12
                    GroupByType.Year => 0, // placeholder, not needed for Year mode
                    _ => b.PickupDateTime.Month
                };

                string periodLabel = groupBy switch
                {
                    GroupByType.Month => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(b.PickupDateTime.Month),
                    GroupByType.Year => "Year",
                    _ => b.PickupDateTime.Month.ToString("D2")
                };

                return new
                {
                    b.Scope,
                    Year = year,
                    PeriodKey = periodKey,
                    PeriodLabel = periodLabel
                };
            });

            var grouped = transformed
                .GroupBy(x => new { x.PeriodKey, x.Scope })
                .Select(g =>
                {
                    var current = g.Count(x => x.Year == startYear);
                    var previous = g.Count(x => x.Year == startYear - 1);
                    var label = g.First().PeriodLabel;

                    double growth = previous == 0
                        ? (current > 0 ? 100 : 0)
                        : Math.Round(((double)(current - previous) / previous) * 100, 2);

                    return new ScopeTimeGrowthDto
                    {
                        PeriodLabel = label,
                        Scope = g.Key.Scope,
                        CurrentYearCount = current,
                        PreviousYearCount = previous,
                        PercentageGrowth = growth
                    };
                })
                .OrderBy(r => r.PeriodLabel)
                .ThenBy(r => r.Scope)
                .ToList();

            return grouped;
        }
        // GROWTH

        public async Task<List<RevenueByMonthDto>> RevenueByMonth(DateTime fromDate, DateTime toDate)
        {
            var rawData = await _dB.AccountInvoices
                .Where(i => i.Date >= fromDate && i.Date <= toDate)
                .GroupBy(i => new { i.Date.Year, i.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    NetTotal = g.Sum(i => i.NetTotal)
                })
                .ToListAsync();

            var result = rawData
                .Select(g => new RevenueByMonthDto
                {
                    Month = $"{g.Year:D4}-{g.Month:D2}",
                    NetTotal = g.NetTotal
                })
                .OrderBy(r => r.Month)
                .ToList();

            return result;
        }


        public async Task<List<PayoutByMonthDto>> PayoutsByMonth(DateTime fromDate, DateTime toDate)
        {
            var rawData = await _dB.DriverInvoiceStatements
                .Where(s => s.DateCreated <= toDate && s.DateCreated >= fromDate)
                .GroupBy(s => new { s.DateCreated.Year, s.DateCreated.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalPaymentDue = g.Sum(s => (decimal)(s.EarningsAccount + s.EarningsCard - s.CommissionDue))
                })
                .ToListAsync();

            var result = rawData
                .Select(g => new PayoutByMonthDto
                {
                    Month = $"{g.Year:D4}-{g.Month:D2}",
                    TotalPaymentDue = g.TotalPaymentDue
                })
                .OrderBy(r => r.Month)
                .ToList();

            return result;
        }

        public async Task<List<ProfitabilityDto>> ProfitabilityOnInvoices(DateTime fromDate, DateTime toDate)
        {
            var lst = new List<ProfitabilityDto>();

            var invoices = await _dB.AccountInvoices
                .Where(o=>o.Date.Date >= fromDate.Date && o.Date.Date <= toDate.Date)
                .AsNoTracking()
                .Select(o => new { o.InvoiceNumber, o.AccountNo, o.Date, o.NetTotal,})
                .ToListAsync();

            foreach (var invoice in invoices) 
            {
                var cost = await _dB.Bookings.Where(o => o.InvoiceNumber == invoice.InvoiceNumber && o.Cancelled == false)
                    .AsNoTracking()
                    .SumAsync(o => o.Price);

                var profit = invoice.NetTotal - cost;

                double margin = invoice.NetTotal > 0
                   ? (double)profit / (double)invoice.NetTotal
                   : 0; // Avoid division by zero

                lst.Add(new ProfitabilityDto
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    AccountNo = invoice.AccountNo,
                    Date = invoice.Date,
                    NetTotal = invoice.NetTotal,
                    Cost = cost,
                    Profit = (double)profit,
                    Margin = Math.Round((margin * 100), 2)
                });
            }

            return lst;
        }

        public async Task<dynamic> TotalProfitabilityByPeriod(DateTime fromDate, DateTime toDate)
        {
            var invoices = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
                 (o.Scope == BookingScope.Account) && o.AccountNumber != 10027 && 
                 (o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date) &&
                 (o.UserId != null))
                 .AsNoTracking()
                 .Select(o => new { o.Price, o.PriceAccount, o.Scope })
                 .ToListAsync();


            var data = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
                 (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Rank || o.Scope == BookingScope.Card) &&
                 (o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date) &&
                 (o.UserId != null))
                 .AsNoTracking()
                 .Select(o => new { o.Price, o.Scope })
                 .ToListAsync();

            var grp = data.GroupBy(o => o.Scope).ToList();

            decimal commsTotal = 0;

            foreach (var scope in grp)
            {
                var lst = scope.ToList();
                var total = lst.Sum(o => o.Price);
                var comms = total * .15M;  // 15 % of total 
                var vat = (comms / 100) * 20;

                var sum = comms - vat;
                commsTotal += sum;
            }

            // calculate total invoices for period
            var salesTotal = invoices.Sum(o => o.PriceAccount);
            var driverPayoutsTotal = invoices.Sum (o => o.Price);
            
            var staffPayouts = await _dB.Bookings
                .Where(o => o.PickupAddress.Contains("Phone") && (o.UserId == 9 || o.UserId == 25) &&
                    o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date)
                //.Select(o => new { o.PickupDateTime, o.Price })
                //.ToListAsync();
                .SumAsync(o => o.Price);


            var minus = driverPayoutsTotal + staffPayouts;

            var profitTotal = (salesTotal + commsTotal) - minus;

            double margin = (salesTotal + commsTotal) > 0
             ? (double)profitTotal / (double)(salesTotal + commsTotal)
                : 0; // Avoid division by zero

            var marginPercentage = Math.Round((margin * 100), 2);

            var res = new { 
                Period = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}",
                CommissionNet = Math.Round(commsTotal,2),
                SalesTotal = Math.Round(salesTotal,2),
                DriverPayoutsTotal= Math.Round(driverPayoutsTotal,2),
                StaffPayoutsTotal = Math.Round(staffPayouts, 2),
                TotalPayouts = Math.Round(minus, 2),
                TotalProfit = Math.Round(profitTotal, 2),
                Margin = marginPercentage
            };

            return res;
        }
        // cash profs gross | rank profs gross | acc profs net | acc margin % | phones costs | net comms profs | gross comms profits | net profits |
        public record ProfitabilitySummary
        {
            // ACCOUNTS
            public decimal AccountSales { get; set; } // from customers
            public decimal AccountPayouts { get; set; } // to drivers

            public decimal TotalAccountProfitNet { get { return AccountSales - AccountPayouts; } }
            public decimal AccountProfitMargin { get; set; }

            // CASH & RANK
            public decimal CashProfitsGross { get; set; }
            public decimal CashProfitsNet { get; set; }
            public decimal RankProfitsGross { get; set; }
            public decimal RankProfitsNet { get; set; }
            
            public decimal TotalCommsProfitNet { get { return RankProfitsNet + CashProfitsNet; } }
            public decimal TotalCommsProfitGross { get { return RankProfitsGross + CashProfitsGross; } }

            // EXPENSES
            public decimal TotalCosts { get; set; }

            public decimal NetProfit { get { return (TotalCommsProfitNet + TotalAccountProfitNet) - TotalCosts; } }
            public decimal GrossProfit { get { return (TotalCommsProfitNet + TotalAccountProfitNet); } }
        }

        public async Task<ProfitabilitySummary> GetProfitsByDateRange(DateTime fromDate, DateTime toDate)
        {
            var accData = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
                (o.Scope == BookingScope.Account) && o.AccountNumber != 10027 &&
                (o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date) &&
                (o.UserId != null))
                .AsNoTracking()
                .Select(o => new { o.Price, o.PriceAccount, o.PickupDateTime, o.PassengerName })
                .ToListAsync();


            var costs = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
                (o.Scope == BookingScope.Account) && o.AccountNumber == 10027 &&
                (o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date) &&
                (o.UserId != null))
                .AsNoTracking()
                .Select(o => new { o.Price, o.PriceAccount })
                .ToListAsync();

            var cash = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
              (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Rank || o.Scope == BookingScope.Card) &&
              (o.PickupDateTime.Date >= fromDate.Date && o.PickupDateTime.Date <= toDate.Date) &&
              (o.UserId != null))
              .AsNoTracking()
              .Select(o => new { o.Price, o.Scope })
              .ToListAsync();


            // calculate account profitabiity
            var accSalesTotal = accData.Sum(o => o.PriceAccount);
            var accDriverPayoutsTotal = accData.Sum(o => o.Price);

            // calculate cash profitability
            var csh = cash.Where(o => o.Scope == BookingScope.Cash).ToList();
            var rnk = cash.Where(o => o.Scope == BookingScope.Rank).ToList();

            var cashTotal = csh.Sum(o => o.Price);
            var cashComms = cashTotal * .15M;  // 15 % of total

            var rankTotal = rnk.Sum(o => o.Price);
            var rankComms = rankTotal * 0.075M;  // 7.5 % of total

            var cashRankCommsTotal = cashComms + rankComms;
            var cashRankCommsNet = cashRankCommsTotal - ((cashRankCommsTotal / 100) * 20); // minus vat

            // calculate costs 
            var costsTotal = costs.Sum(o => o.Price);

            var res = new ProfitabilitySummary 
            {
                AccountSales = Math.Round(accSalesTotal,2),
                AccountPayouts = Math.Round(accDriverPayoutsTotal,2),
                AccountProfitMargin = accSalesTotal > 0 ? Math.Round(((accSalesTotal - accDriverPayoutsTotal) / accSalesTotal) * 100, 2) : 0,
                CashProfitsGross = Math.Round(cashComms,2),
                CashProfitsNet = Math.Round(cashComms - ((cashComms / 100) * 20),2),
                RankProfitsGross = Math.Round(rankComms,2),
                RankProfitsNet = Math.Round(rankComms - ((rankComms / 100) * 20),2),
                TotalCosts = Math.Round(costsTotal, 2)
            };

            return res;
        }


        public record QRCodeScanSummary
        {
            public string Location { get; set; }
            public int ScanCount { get; set; }
            public DateTime FirstScan { get; set; }
            public DateTime LastScan { get; set; }
        }
        public async Task<List<QRCodeScanSummary>> GetQRCodeScans()
        { 
            var scans = await _dB.QRCodeClicks.ToListAsync();
            var grp = scans.GroupBy(o => o.Location).ToList();

            var res = new List<QRCodeScanSummary>();

            foreach (var g in grp)
            {
                var lst = g.ToList();
                var count = lst.Count;
                var first = lst.Min(o => o.TimeStamp);
                var last = lst.Max(o => o.TimeStamp);

                var obj = new QRCodeScanSummary
                {
                    Location = g.Key,
                    ScanCount = count,
                    FirstScan = first,
                    LastScan = last
                };

                res.Add(obj);
            }

            return res;
        }

        #endregion

    }
}


