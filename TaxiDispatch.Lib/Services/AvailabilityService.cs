using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.Admin;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.User;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services
{
    public class AvailabilityService : BaseService<AvailabilityService>
    {
        private readonly IMapper _mapper;
        public AvailabilityService(IMapper mapper,
            IDbContextFactory<TaxiDispatchContext> factory, ILogger<AvailabilityService> logger) 
            : base(factory,logger)
        {
            _mapper = mapper;
        }

        public async Task<Result> ValidateCreate(int userId, DateTime date, TimeSpan from, TimeSpan to, DriverAvailabilityType type)
        {
            // Ensure 'from' is earlier than 'to'
            if (from >= to)
            {
                // throw new ArgumentException("The starting time must be earlier than the ending time.");
                return Result.Fail("The starting time must be earlier than the ending time.");
            }

            // Get any existing availabilities for the user on the specified date
            var exists = await _dB.DriverAvailabilities
                .Where(o => o.UserId == userId && o.Date.Date == date.Date)
                .ToListAsync();

            // Check for time overlaps and exact matches
            foreach (var availability in exists)
            {
                // Assuming availability.FromTime and availability.ToTime are TimeSpan
                if ((from < availability.ToTime && to > availability.FromTime && availability.Date == date.Date))
                {
                    return Result.Fail("The specified time overlaps with an existing availability.");
                }

                // Check if an exact match exists for the requested time
                if (from == availability.FromTime && to == availability.ToTime && availability.Date == date.Date)
                {
                    return Result.Fail("An availability entry for the specified time already exists.");
                }
            }

            // If there are no overlaps and no exact matches, proceed with your logic (e.g., creating new availability)
            return Result.Ok();
        }

        public async Task<Result> Create(int userId, DateTime date, TimeSpan from, TimeSpan to, bool giveOrTake,
            DriverAvailabilityType type, string note, string byUsername)
        {
            var res = await ValidateCreate(userId, date, from, to, type);

            if (res.Success)
            {
                if (note == null)
                    note = string.Empty;

                var time = DateTime.Now.ToUKTime();

                await _dB.DriverAvailabilities.AddAsync(new Data.Models.DriverAvailability
                {
                    UserId = userId,
                    Date = date.Date,
                    AvailabilityType = type,
                    FromTime = from,
                    ToTime = to,
                    Description = note,
                    GiveOrTake = giveOrTake
                });

                await _dB.DriverAvailabilityAudits.AddAsync(new Data.Models.DriverAvailabilityAudit 
                {
                    UserId = userId,
                    ForDate = date.Date, 
                    ChangedOn = time,
                    TheChange = $"Created - {from.ToString(@"hh\:mm")} - {to.ToString(@"hh\:mm")} GOT: {giveOrTake} | {note}",
                    ChangeBy = byUsername
                });

                await _dB.SaveChangesAsync();
                return res;
            }

            return res;
        }

        public async Task Delete(int id, string byUsername)
        {
            var obj = await _dB.DriverAvailabilities.Where(x => x.Id == id).FirstOrDefaultAsync();

            if (obj != null)
            { 
                _dB.DriverAvailabilities.Remove(obj);

                await _dB.DriverAvailabilityAudits.AddAsync(new Data.Models.DriverAvailabilityAudit
                {
                    UserId = obj.UserId,
                    ForDate = obj.Date,
                    ChangedOn = DateTime.Now.ToUKTime(),
                    TheChange = $"Deleted - {obj.FromTime.ToString(@"hh\:mm")} - {obj.ToTime.ToString(@"hh\:mm")}",
                    ChangeBy = byUsername
                });

                await _dB.SaveChangesAsync();
            }
        }

        public async Task<DriverAvailabilitiesDto> GetAvailabilities(DateTime date)
        {
            try
            {
                var data = await _dB.DriverAvailabilities
                .Where(o => o.Date.Date == date.Date.Date)
                .AsNoTracking()
                .OrderBy(o => o.Description)
                .ToListAsync();

                var res = new DriverAvailabilitiesDto();

                var lst = await _dB.UserProfiles.AsNoTracking().Include(o => o.User).Select(o => new
                {
                    UserId = o.UserId,
                    Color = o.ColorCodeRGB,
                    Fullname = o.User.FullName
                })
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var item in data)
                {
                    var obj = lst.Where(o => o.UserId == item.UserId).FirstOrDefault();

                    var obj1 = new DriverAvailabilityDto
                    {
                        UserId = item.UserId,
                        Date = item.Date,
                        Description = item.Description,
                        FullName = obj.Fullname, 
                        Id = item.Id, 
                        AvailabilityType = item.AvailabilityType, 
                        From = item.FromTime, 
                        To = item.ToTime, 
                        GiveOrTake = item.GiveOrTake
                    };

                    obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                    res.Drivers.Add(obj1);
                }

                return res;
            }
            catch (Exception)
            {
                // expected on refresh
                return new DriverAvailabilitiesDto { };
            }

        }
        public async Task<DriverAvailabilitiesDto> GetAvailabilities(int userid, DateTime? date)
        {
            var data = new List<DriverAvailability>();

            if (date == null)
            {
                data = await _dB.DriverAvailabilities
                    .Where(o => o.UserId == userid && o.Date > DateTime.Now.Date.AddDays(-1))
                    .AsNoTracking()
                    .OrderBy(o => o.Description)
                    .ToListAsync();
            }
            else
            {
                data = await _dB.DriverAvailabilities
                    .Where(o => o.UserId == userid && o.Date == date.Value.Date)
                    .AsNoTracking()
                    .OrderBy(o => o.Description)
                    .ToListAsync();
            }
            

            var res = new DriverAvailabilitiesDto();

            var lst = await _dB.UserProfiles.Where(o => o.UserId == userid)
                .AsNoTracking().Include(o => o.User).Select(o => new {
                    UserId = o.UserId,
                    Color = o.ColorCodeRGB,
                    Fullname = o.User.FullName
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in data)
            {
                var obj = lst.Where(o => o.UserId == item.UserId).FirstOrDefault();

                var obj1 = new DriverAvailabilityDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Date = item.Date,
                    Description = item.Description,
                    FullName = obj.Fullname, 
                    From = item.FromTime,
                    To = item.ToTime, 
                    AvailabilityType = item.AvailabilityType, 
                    GiveOrTake = item.GiveOrTake
                };

                obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                res.Drivers.Add(obj1);
            }

            return res;
        }

        public async Task<List<DriverAvailabilityAudit>> GetAuditLog(DateTime date)
        {
            var data  = await _dB.DriverAvailabilityAudits.Where(o => o.ForDate.Date == date.Date).ToListAsync();
            return data;
        }
        public async Task<List<DriverAvailabilityAudit>> GetAuditLog(DateTime date, int userId)
        {
            var data = await _dB.DriverAvailabilityAudits
                .Where(o => o.ForDate.Date == date.Date && o.UserId == userId).ToListAsync();
            return data;
        }

        public async Task<List<Availability>> GetGeneralAvailability(DateTime date)
        {
            try
            {
                var data = await _dB.DriverAvailabilities
                .Where(o => o.Date.Date == date.Date.Date)
                .AsNoTracking()
                .OrderBy(o => o.Description)
                .ToListAsync();

                var res = new List<Availability>();

                var lst = await _dB.UserProfiles.AsNoTracking().Include(o => o.User).Select(o => new
                {
                    UserId = o.UserId,
                    Color = o.ColorCodeRGB,
                    Fullname = o.User.FullName,
                    Vehicle = o.VehicleType
                })
                    .AsNoTracking()
                    .ToListAsync();

                // group by driver
                var grp = data.GroupBy(o => o.UserId).ToList();

                foreach (var drvr in grp)
                {
                    var obj = lst.Where(o => o.UserId == drvr.Key).FirstOrDefault();

                    var entrys = drvr.ToList();

                    var obj1 = new Availability
                    {
                        UserId = drvr.Key,
                        Date = date.Date,
                        FullName = obj.Fullname,
                        VehicleType = obj.Vehicle
                    };

                    // build drivers available/unavailble hours
                    foreach (var entry in entrys) 
                    {
                        if (entry.AvailabilityType == DriverAvailabilityType.Available)
                        {
                            obj1.AvailableHours.Add(new Hours { From = entry.FromTime, To = entry.ToTime, Note = entry.Description });
                        }
                        else
                        {
                            obj1.UnAvailableHours.Add(new Hours { From = entry.FromTime, To = entry.ToTime, Note = entry.Description });
                        }
                    }

                    // add driver color
                    obj1.ColorCode = lst.FirstOrDefault(o => o.UserId == obj.UserId).Color;
                    
                    // add to results object
                    res.Add(obj1);
                }

                return res;
            }
            catch (Exception)
            {
                // expected on refresh
                return new List<Availability> { };
            }
        }
        public async Task<List<Availability>> PossibleAvailableDrivers(DateTime pickupDateTime, string pickupPostcode, string[] viaPostcodes,
            string destinationPostcode, int passengers)
        {
            // get all drivers availabilities
            var drivers = await GetGeneralAvailability(pickupDateTime.Date);

            // remove any unavailable drivers
            drivers = drivers.Where(o=>o.AvailableHours.Count > 0).ToList();

            // check if mpv only
            if(passengers >= 5)
            {
                // remove from availabilities drivers not MPV
                drivers = drivers.Where(o=>o.VehicleType != VehicleType.MPV).ToList();
            }

            // get jobs later than requested pickup date/time on same day
            var jobs = await _dB.Bookings
                .Where(o => o.PickupDateTime > pickupDateTime && o.PickupDateTime.Date == pickupDateTime.Date &&
                    o.UserId != null && o.Cancelled == false && o.Status != BookingStatus.Complete)
                .Select(o => 
                    new { 
                        o.PickupDateTime, 
                        o.UserId, 
                        o.PickupPostCode,
                        o.Passengers
                    })
                .ToListAsync();

            /*
             * - we have a list of drivers that are available for the required pickup time/date with the correct vehicle
             * - we have a list of bookings that have been allocated to drivers later than required time
             */

            // calculate journey time for required trip

            // remove any driver not able to make their next pre-allocated journey on time

            return null;
        }
        public async Task<AvailabilityReportDto> GetAvailabilityForPeriod(DateTime from, DateTime to, int userId = 0)
        {
            var report = new AvailabilityReportDto();

            List<DriverAvailability> data = null;

            if (userId == 0)
            {
                data = await _dB.DriverAvailabilities
                  .Where(o => o.Date.Date >= from.Date && o.Date.Date <= to.Date && o.AvailabilityType == DriverAvailabilityType.Available)
                  .AsNoTracking()
                  .OrderBy(o => o.Date)
                  .ToListAsync();
            }
            else
            {
                data = await _dB.DriverAvailabilities
                  .Where(o => o.Date.Date >= from.Date && o.Date.Date <= to.Date &&
                    o.AvailabilityType == DriverAvailabilityType.Available && o.UserId == userId)
                  .AsNoTracking()
                  .OrderBy(o => o.Date)
                  .ToListAsync();
            }

            // group by date
            var dategrp = data.GroupBy(o => o.Date).ToList();

            foreach (var date in dategrp) 
            {
                // group by user
                var usergrp = date.GroupBy(o => o.UserId).ToList();

                // get total for each user
                foreach (var user in usergrp)
                {
                    var available = user.ToList();
                    var userHours = new AvailableHours();

                    userHours.Date = date.Key;
                    userHours.UserId = user.Key;

                    // get total hours for user for the date
                    foreach (var entry in available)
                    {
                        var hours = entry.FromTime - entry.ToTime;
                        
                        userHours.HoursAvailable += Math.Abs((double)hours.TotalHours);
                    }

                    // format 
                    userHours.HoursAvailable = Math.Round(userHours.HoursAvailable);

                    // add user hours for date
                    report.AvailableHoursByDay.Add(userHours);
                }
            }

            // calculate range totals
            var ah = new AvailableHours();
            
            var tmonth = ah.CalculateTotalHoursByMonth(report.AvailableHoursByDay);
            var tweek = ah.CalculateTotalHoursByWeek(report.AvailableHoursByDay);
            var tweekday = ah.CalculateTotalHoursByWeekday(report.AvailableHoursByDay);
            var tweekend = ah.CalculateTotalHoursByWeekend(report.AvailableHoursByDay);
            
            // remove from week day
            tweekday.RemoveAll(o => o.Day == DayOfWeek.Saturday || o.Day == DayOfWeek.Sunday);

            report.Month = tmonth;
            report.Week = tweek;
            report.WeekDay = tweekday;
            report.WeekEnd = tweekend;

            // unavailable hours
            var alldates = new List<DateTime>();
            while (from.Date <= to.Date)
            {
                alldates.Add(from.Date);
                from = from.AddDays(1);
            }

            var userids = data.Select(o => o.UserId).Distinct().ToList();

            foreach (var user in userids)
            {
                var o = new UnAvailableDatesDto
                {
                    UserId = user
                };

                foreach (var date in alldates)
                {
                    var cnt = data.Where(o => o.Date.Date == date.Date && o.UserId == user).Count();

                    if (cnt == 0)
                    {
                        o.UnAvailableDates.Add(date.Date);
                    }
                }

                report.Unavailable.Add(o);
            }

            return report;
        }


        #region ON SHIFT

        public async Task<List<DriverOnShiftDto>> GetOnShiftDrivers()
        {
            var date = DateTime.Now.Date;
            var data = await _dB.DriversOnShift.Where(o => o.StartAt.Date == date).ToListAsync();

            var drivers = data.Select(o=>o.UserId).ToList();

            var colors = await _dB.UserProfiles.Include(o=>o.User)
                .Where(o=>drivers.Contains(o.UserId))
                .Select(o => new { o.UserId, ColorCode = o.ColorCodeRGB, 
                    o.User.FullName,
                    o.VehicleMake, 
                    o.VehicleModel, 
                    o.VehicleColour, 
                    o.RegNo})
                .ToListAsync();

            var res = _mapper.Map<List<DriverOnShift>, List<DriverOnShiftDto>>(data);

            foreach (var item in res)
            {
                var clr = colors.FirstOrDefault(o=>o.UserId == item.UserId);

                item.ColourCode = clr.ColorCode;
                item.Fullname = clr.FullName;
                item.VehicleMake = clr.VehicleMake;
                item.VehicleModel = clr.VehicleModel;
                item.VehicleColour = clr.VehicleColour;
                item.VehicleReg = clr.RegNo;
            }

            return res;
        }

        #endregion
    }
}


