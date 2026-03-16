#nullable disable
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.PDF;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiDispatch.Modules.Messaging.Services;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Result = TaxiDispatch.Domain.Result;


namespace TaxiDispatch.Services
{
    public partial class BookingService : BaseService<BookingService>
    {
        private readonly IMapper _mapper;
        private readonly IPushNotificationService _notificationService;
        private readonly UserProfileService _usersService;
        private readonly AceMessagingService _messageService;
        private readonly TariffService _tarrifService;
        private readonly DispatchService _dispatchService;

        public BookingService(
            IDbContextFactory<TaxiDispatchContext> factory,
            IMapper mapper,
            IPushNotificationService notificationService,
            TariffService tariffService,
            UserProfileService usersService, 
            AceMessagingService messagingService,
            DispatchService dispatchService,
            ILogger<BookingService> logger)
            : base(factory,logger)
        {
            _mapper = mapper;
            _notificationService = notificationService;
            _usersService = usersService;
            _messageService = messagingService;
            _tarrifService = tariffService;
            _dispatchService = dispatchService;
        }

        public async Task<GetBookingsResponseDto> GetBookingsToday()
        {
            var data = await _dB.Bookings.Include(o=>o.Vias).Where(o =>
                o.PickupDateTime.Date == DateTime.Now.Date &&
                o.Cancelled == false)
                .AsNoTracking()
                .ToListAsync();

            var res = new GetBookingsResponseDto();

            foreach (var item in data)
            {
                var color = Constants.UnAllocatedColor;
                var regno = string.Empty;
                var dname = string.Empty;

                if (item.UserId != null)
                {
                    var profile = await _dB.UserProfiles.Include(o=>o.User).Where(o => o.UserId == item.UserId).FirstOrDefaultAsync();

                    color = profile.ColorCodeRGB;
                    regno = profile.RegNo;
                    dname = profile.User.FullName;
                }

                var model = _mapper.Map<Booking, PersistedBookingModel>(item);
                model.BackgroundColorRGB = color;
                model.Fullname = dname;
                model.RegNo = regno;
                model.BookingId = item.Id;

                model.Vias = model.Vias?.OrderBy(o => o.ViaSequence).ToList();

                res.Bookings.Add(model);
            }

            return res;
        }

        public async Task<GetBookingsResponseDto> GetBookingsDash(DateTime startDate, DateTime? endDate, int userId, bool includeCancelled)
        {
            var data = await GetBookings(startDate, endDate, userId, true);

            if (!includeCancelled)
                data.Bookings = data.Bookings.Where(o => o.Cancelled == false).ToList();

            return data;
        }

        public async Task<GetBookingsResponseDto> GetBookingsByDriver(DateTime startDate, DateTime? endDate, int userId)
        {
            var res = new GetBookingsResponseDto();
            if (!endDate.HasValue)
                endDate = DateTime.Now.AddDays(1);

            var data = await _dB.Bookings
                           .Include(o => o.Vias)
                           .AsNoTracking()
                           .Where(o => o.PickupDateTime >= startDate.Date && o.Cancelled == false &&
                           o.PickupDateTime < endDate && o.UserId == userId).OrderBy(o=>o.PickupDateTime)
                           .ToListAsync();

            foreach (var item in data)
            {
                var model = _mapper.Map<Booking, PersistedBookingModel>(item);
                model.BookingId = item.Id;
                
                if(model.Status == null)
                {
                    model.Status = BookingStatus.None;
                }

                res.Bookings.Add(model);
            }

            return res;
        }

        public async Task<GetBookingsResponseDto> GetBookings(DateTime startDate, DateTime? endDate, int userId, bool dash = false)
        {
            if (!endDate.HasValue)
                endDate = DateTime.Now.AddDays(1);

            List<Booking> data = null;
            List<Booking> hvs = null;

            // check if user can see all
            var callerProfile = await _dB.UserProfiles.Include(o=>o.User).Where(o => o.UserId == userId).FirstOrDefaultAsync();

            if (!dash) // API CALL
            {
                if (callerProfile.ShowAllBookings)
                {
                    data = await _dB.Bookings
                        .Include(o => o.Vias)
                        .AsNoTracking()
                        .Where(o => o.PickupDateTime >= startDate.Date && o.Cancelled == false &&
                        o.PickupDateTime < endDate).ToListAsync();
                }
                else // own jobs only
                {
                    data = await _dB.Bookings
                            .Include(o => o.Vias)
                            .AsNoTracking()
                            .Where(o => o.PickupDateTime >= startDate.Date && o.Cancelled == false &&
                            o.PickupDateTime < endDate && o.UserId == userId).ToListAsync();

                    if (callerProfile.ShowHVSBookings)
                    {
                        hvs = await _dB.Bookings
                            .Include(o => o.Vias)
                            .AsNoTracking()
                            .Where(o => 
                            o.PickupDateTime >= startDate.Date && 
                            o.Cancelled == false &&
                            (o.AccountNumber == 9014 || o.AccountNumber == 10026) &&
                            o.PickupDateTime < endDate)
                            .ToListAsync();

                        var remove = hvs.Where(o=>o.UserId == userId).ToList();
                        
                        foreach (var item in remove)
                        {
                            hvs.Remove(item);
                        }
                    }
                }
            }
            else
            {
                data = await _dB.Bookings
                    .Include(o => o.Vias)
                    .AsNoTracking()
                    .Where(o => o.PickupDateTime >= startDate.Date && o.PickupDateTime < endDate).ToListAsync();
            }

            var res = new GetBookingsResponseDto();

            // gets driver colors for HVS bookings
            if (hvs != null)
            {
                foreach (var item in hvs)
                {
                    var color = Constants.UnAllocatedColor;
                    var regno = string.Empty;
                    var dname = string.Empty;

                    // get user data per job
                    if (item.UserId != null && item.UserId != -1)
                    {
                        if (item.UserId == 0)
                        {
                            throw new Exception($"The booking {item.Id} has an invalid userId");
                        }

                        var profile = await _dB.UserProfiles.Include(o => o.User).Where(o => o.UserId == item.UserId).FirstOrDefaultAsync();
                        color = profile.ColorCodeRGB;
                        regno = profile.RegNo;
                        dname = profile.User.FullName;
                    }


                    var model = _mapper.Map<Booking, PersistedBookingModel>(item);
                    model.ManuallyPriced = item.ManuallyPriced;
                    model.BackgroundColorRGB = color;
                    model.Fullname = dname;
                    model.RegNo = regno;
                    model.BookingId = item.Id;

                    model.Vias = model.Vias?.OrderBy(o => o.ViaSequence).ToList();

                    if (item.Status == BookingStatus.AcceptedJob)
                    {
                        model.Status = BookingStatus.AcceptedJob;
                    }
                    if (item.Status == BookingStatus.RejectedJob)
                    {
                        model.Status = BookingStatus.RejectedJob;
                    }

                    model.Vias?.ForEach(o => o.Booking = null);

                    res.Bookings.Add(model);
                }
            }

            foreach (var item in data)
            {
                var color = Constants.UnAllocatedColor; //"#e3e3e3";
                var regno = string.Empty;
                var dname = string.Empty;

                if (callerProfile.ShowAllBookings)
                {
                    // get user data per job
                    if (item.UserId != null && item.UserId != -1)
                    {
                        if (item.UserId == 0)
                        {
                            throw new Exception($"The booking {item.Id} has an invalid userId");
                        }

                        var profile = await _dB.UserProfiles.Include(o => o.User).Where(o => o.UserId == item.UserId).FirstOrDefaultAsync();
                        color = profile.ColorCodeRGB;
                        regno = profile.RegNo;
                        dname = profile.User.FullName;
                    }
                }
                else
                {
                    color = callerProfile.ColorCodeRGB;
                    regno = callerProfile.RegNo;
                    dname = callerProfile.User.FullName;
                }

                var model = _mapper.Map<Booking, PersistedBookingModel>(item);
                model.ManuallyPriced = item.ManuallyPriced;
                model.BackgroundColorRGB = color;
                model.Fullname = dname;
                model.RegNo = regno;
                model.BookingId = item.Id;

                model.Vias = model.Vias?.OrderBy(o => o.ViaSequence).ToList();

                if (item.Status == BookingStatus.AcceptedJob)
                {
                    model.Status = BookingStatus.AcceptedJob;
                }
                if (item.Status == BookingStatus.RejectedJob)
                {
                    model.Status = BookingStatus.RejectedJob;
                }

                model.Vias?.ForEach(o => o.Booking = null);

                res.Bookings.Add(model);
            }

            return res;
        }
        
        public async Task<PersistedBookingModel?> GetBooking(int bookingId)
        {
            var data = await _dB.Bookings
                .Include(o=>o.Vias)
                .AsNoTracking()
                .Where(o => o.Id == bookingId).FirstOrDefaultAsync();

            data.Vias = data.Vias?.OrderBy(o => o.ViaSequence).ToList();

            data.Vias?.ForEach(o => o.Booking = null);

            var res = _mapper.Map<Booking, PersistedBookingModel>(data);
            res.ManuallyPriced = data.ManuallyPriced;
            res.BookingId = data.Id;

            if (res.UserId != null || res.UserId != 0)
            {
                var fname = await _dB.Users.Where(o => o.Id == res.UserId).Select(o => o.FullName).FirstOrDefaultAsync();
                res.Fullname = fname;
            }
            
            return res;
        }

        public async Task<Booking?> GetBooking2(int bookingId) 
        {

            var data = await _dB.Bookings
                .Include(o => o.Vias)
                .AsNoTracking()
                .Where(o => o.Id == bookingId).FirstOrDefaultAsync();

            data.Vias = data.Vias?.OrderBy(o => o.ViaSequence).ToList();

            var res = _mapper.Map<Booking, PersistedBookingModel>(data);
            res.BookingId = data.Id;
            return data;
        }

        public async Task<List<ActiveBookingDataDto>> GetAccountActiveBookings(int accno)
        { 
            var now = DateTime.Now.AddHours(-6).ToUKTime();

            var data = await _dB.Bookings
                .Include(o => o.Vias)
                .AsNoTracking()
                .Where(o => o.PickupDateTime >= now && o.AccountNumber == accno && o.Cancelled == false)
                .Select(o => new ActiveBookingDataDto
                {
                    BookingId = o.Id,
                    DateTime = o.PickupDateTime,
                    PickupAddress = $"{o.PickupAddress}, {o.PickupPostCode}",
                    DestinationAddress = $"{o.DestinationAddress}, {o.DestinationPostCode}",
                    PassengerName = o.PassengerName, RecurranceId = o.RecurrenceID
                }).OrderBy(o => o.DateTime)
                .ToListAsync();

            return data;
        }

        public async Task<List<CreatedBookingResultDto>> CreateBooking(CreateBookingRequestDto obj, bool allocate = true)
        {
            var createdList = new List<CreatedBookingResultDto>();

            var booking = _mapper.Map<Booking>(obj);
            var date = DateTime.Now.ToUKTime();
            booking.DateCreated = date;

            _logger.LogInformation($"Booking Creation: Duration = {obj.DurationMinutes} | Miles: {obj.MileageText}");

            // copy value
            booking.ManuallyPriced = obj.ManuallyPriced;

            // clear payment status
            booking.PaymentStatus = PaymentStatus.Select;

            // standardize phone
            if(!string.IsNullOrEmpty(booking.PhoneNumber))
                booking.PhoneNumber = booking.PhoneNumber.Trim(' ');

            // standardize postocodes & address
            booking.PickupPostCode = StandardizePostocde(booking.PickupPostCode);
            booking.PickupAddress = booking.PickupAddress.RemoveExtraSpaces();

            booking.DestinationPostCode = StandardizePostocde(booking.DestinationPostCode);
            booking.DestinationAddress = booking.DestinationAddress.RemoveExtraSpaces();

            List<string> viasout = new List<string>();

            if (obj.Vias != null)
            {
                viasout = obj.Vias.Select(o => o.PostCode).ToList();
            }

            try
            {
                var price1 = await _tarrifService.Get9999CashPrice(obj.PickupDateTime, obj.Passengers, obj.PickupPostCode,
                             obj.DestinationPostCode, viasout, obj.ChargeFromBase);

                booking.Mileage = (decimal?)price1.TotalMileage;
                booking.MileageText = price1.MileageText;
            }
            catch(Exception ex)
            {
                _logger.LogError("unable to calculate mileage",ex);
            }

            if (booking.Vias != null)
            { 
                booking.Vias.ForEach(o => 
                {
                    o.Address = o.Address.RemoveExtraSpaces();
                    o.PostCode = StandardizePostocde(o.PostCode);
                }); 
            }

            if (!string.IsNullOrEmpty(booking.RecurrenceRule))
            {
                var created = await CreateBlockBooking(booking);
                createdList.AddRange(created.ResultDtos);
            }
            else // single
            {
                _dB.Bookings.Add(booking);
                await _dB.SaveChangesAsync();

                if (booking.UserId != 0 || booking.UserId != null)
                {
                    if (booking.UserId.HasValue && allocate)
                    {
                        await _dispatchService.AllocateBooking(new AllocateBookingDto 
                        { 
                            BookingId = booking.Id,
                            UserId = booking.UserId.Value,
                            ActionByUserId = booking.ActionByUserId
                        });
                    }
                }

                createdList.Add(new CreatedBookingResultDto
                {
                    BookingId = booking.Id,
                    Date = booking.PickupDateTime,
                    BookedBy = booking.BookedByName,
                    Passenger = booking.PassengerName,
                    AccNo = booking.AccountNumber.Value
                });
            }

            if (obj.ReturnDateTime != null)
            {
                var created = await ReturnBooking(date, booking, obj.ReturnDateTime.Value);
                createdList.AddRange(created);
            }

            return createdList;
        }

        private async Task<List<CreatedBookingResultDto>> ReturnBooking(DateTime createdDate,Booking booking, DateTime returnDateTime)
        {
            var createdList = new List<CreatedBookingResultDto>();

            var returnBooking = ReverseJourney(booking, returnDateTime);
            returnBooking.DateCreated = createdDate;
            returnBooking.IsASAP = false;
            returnBooking.ArriveBy = null;

            if (returnBooking.Scope != BookingScope.Account) // cash
            {
                {
                    List<string> vias = new List<string>();

                    if (returnBooking.Vias != null)
                    {
                        vias = returnBooking.Vias.Select(o => o.PostCode).ToList();
                    }

                    try
                    {
                        var price = await _tarrifService.Get9999CashPrice(returnBooking.PickupDateTime, returnBooking.Passengers, returnBooking.PickupPostCode,
                            returnBooking.DestinationPostCode, vias, returnBooking.ChargeFromBase);

                        if (price != null)
                        {
                            if (!returnBooking.ManuallyPriced)
                                returnBooking.Price = (decimal)price.PriceAccount;

                            returnBooking.Mileage = (decimal)price.TotalMileage;
                            returnBooking.DurationMinutes = price.TotalMinutes;
                            returnBooking.DurationText = price.DurationText;
                            returnBooking.MileageText = price.MileageText;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to get price for return journey.");
                    }
                }
            }
            else // acount - get duration minutes
            {
                List<string> vias = new List<string>();

                if (returnBooking.Vias != null)
                {
                    vias = returnBooking.Vias.Select(o => o.PostCode).ToList();
                }

                var price = await _tarrifService.Get9999CashPrice(returnBooking.PickupDateTime, returnBooking.Passengers, returnBooking.PickupPostCode,
                              returnBooking.DestinationPostCode, vias, returnBooking.ChargeFromBase);

                if (price != null)
                {
                    returnBooking.DurationMinutes = price.TotalMinutes; 
                    returnBooking.DurationText = price.DurationText;
                }
            }

            if (!string.IsNullOrEmpty(returnBooking.RecurrenceRule))
            {
                var created = await CreateBlockBooking(returnBooking);
                createdList.AddRange(created.ResultDtos);
            }
            else
            {
                _dB.Bookings.Add(returnBooking);
                await _dB.SaveChangesAsync();

                createdList.Add(new CreatedBookingResultDto
                {
                    BookingId = returnBooking.Id,
                    Date = returnBooking.PickupDateTime,
                    BookedBy = returnBooking.BookedByName,
                    Passenger = returnBooking.PassengerName,
                    AccNo = returnBooking.AccountNumber.Value
                });
            }

            return createdList;
        }

        private string StandardizePostocde(string pc)
        {
            var res = pc.Replace(" ", "");

            switch (pc.Length)
            {
                case 5:
                    {
                        string first = pc.Substring(0, pc.Length - 3); // N1 1AG
                        string second = pc.Substring(2);
                        res = $"{first.ToUpper()} {second.ToUpper()}";
                        break;
                    }
                case 6:
                    {
                        string first = pc.Substring(0, pc.Length - 3); // N22 1AG
                        string second = pc.Substring(3);
                        res = $"{first.ToUpper()} {second.ToUpper()}";
                        break;
                    }
                case 7:
                    {
                        string first = pc.Substring(0, pc.Length - 3); // NW11 1AG
                        string second = pc.Substring(4);
                        res = $"{first.ToUpper()} {second.ToUpper()}";
                        break;
                    }
                default:
                    {
                        res = pc.ToUpper();
                        break;
                    }
            }

            return res.Replace("  "," ");
        }

        private Booking ReverseJourney(Booking obj, DateTime dateTime)
        {
            var res = new Booking();
            res = obj.Clone() as Booking;
            
            res.Id = 0;

            // now reverse
            res.PickupDateTime = dateTime;
            res.PickupAddress = obj.DestinationAddress;
            res.PickupPostCode = obj.DestinationPostCode;
            res.DestinationAddress = obj.PickupAddress;
            res.DestinationPostCode = obj.PickupPostCode;

            var newVias = new List<BookingVia>();

            if (res.Vias.Any())
            {
                foreach (var via in res.Vias)
                {
                    var v = via.Clone() as BookingVia;
                    v.Id = 0;
                    newVias.Add(v);
                }
                
                newVias.Reverse();

                for (int i = 0; i < newVias.Count; i++)
                {
                    newVias[0].ViaSequence = i;
                }
            }

            res.Vias = newVias;

            return res;
        }

        /// <summary>
        /// returns the unique recurance id
        /// </summary>
        /// <param name="booking"></param>
        /// <returns></returns>
        private async Task<(int Recurranceid, List<CreatedBookingResultDto> ResultDtos)> CreateBlockBooking(Booking booking, bool removeOriginalDate = false)
        {
            var createdList = new List<CreatedBookingResultDto>();

            var rule = new BookingRule(booking.RecurrenceRule);
            List<DateTime> dates = rule.GetDateTimes(booking.PickupDateTime);

            if (removeOriginalDate)
            {
                dates.Remove(booking.PickupDateTime);
            }

            List<Booking> bookings = new List<Booking>();
            Random random = new Random();
            var uid = random.Next(Int32.MaxValue);

            // reorder dates
            dates = dates.OrderBy(d => d.Date).ToList();

            // create bookings
            foreach (var date in dates)
            {
                var bookingRepeat = (Booking)booking.Clone();

                bookingRepeat.Id = 0;
                bookingRepeat.PickupDateTime = date;
                bookingRepeat.RecurrenceID = uid; // unique
                bookingRepeat.Vias = null;
                bookingRepeat.UserId = null; // unallocated

                var nvias = new List<BookingVia>();

                if (booking.Vias != null) // reset the booking id and via id
                {
                    foreach (var via in booking.Vias)
                    {
                        var nvia = via.Clone() as BookingVia;
                        nvia.Id = 0;
                        nvia.BookingId = 0;
                        nvias.Add(nvia);
                    }
                }

                // assign new vias
                bookingRepeat.Vias = nvias;
                await _dB.Bookings.AddAsync(bookingRepeat);
                await _dB.SaveChangesAsync();

                createdList.Add(new CreatedBookingResultDto
                {
                    BookingId = bookingRepeat.Id,
                    Date = bookingRepeat.PickupDateTime,
                    BookedBy = bookingRepeat.BookedByName,
                    Passenger = bookingRepeat.PassengerName,
                    AccNo = bookingRepeat.AccountNumber.Value
                });
            }

            return (uid,createdList);
        }

        public async Task<Result<List<CreatedBookingResultDto>>> UpdateBooking(UpdateBookingRequestDto obj)
        {
            _logger.LogInformation($"Booking Edit {obj.BookingId}: Duration = {obj.DurationMinutes} | Miles: {obj.MileageText}");

            var createdList = new List<CreatedBookingResultDto>();

            if (obj.UserId == 0)
                obj.UserId = null;

            if (obj.EditBlock)
            {
                var startDate = _dB.Bookings.AsNoTracking().Where(o => o.Id == obj.BookingId).Select(o => o.PickupDateTime).FirstOrDefault();

                var lst = await _dB.Bookings
                    // .AsNoTracking() -- do not use otherwise we loose audit log
                    .Where(o => 
                        o.RecurrenceID == obj.RecurrenceID &&
                            (o.PickupDateTime >= startDate.ToUKTime()) &&
                            o.Cancelled == false)
                    .OrderBy(o => o.PickupDateTime)
                    .Include(v => v.Vias)
                    .ToListAsync();

                if (lst.Any())
                {
                    // if time changed 
                    var newTime = new TimeSpan(obj.PickupDateTime.Hour, obj.PickupDateTime.Minute, 0);

                    var oldTime = new TimeSpan(lst[0].PickupDateTime.Hour, lst[0].PickupDateTime.Minute,0);

                    var timeChanged = TimeSpan.Compare(oldTime, newTime);

                    // TODO :: Attempt by Ravi---------------------------------------------------------
                    // what if recurrance schedule has changed?
                    var existingRule = lst[0].RecurrenceRule;
                    var currentRule = obj.RecurrenceRule;

                    // check if change in rule
                    if (existingRule != currentRule)
                    {
                        var oldRule = new BookingRule(existingRule);
                        var newRule = new BookingRule(currentRule);
                       
                        var deleteList = new List<Booking>();

                        var dt = lst[0].PickupDateTime;
                        var updatedBooking = UpdateBookingData(lst[0], obj);
                        if (timeChanged != 0)
                        {
                            updatedBooking.PickupDateTime = new DateTime(dt.Year, dt.Month, dt.Day, newTime.Hours, newTime.Minutes, 0);
                        }
                        else
                        {
                            updatedBooking.PickupDateTime = dt;
                        }
                        var newDateList = newRule.GetDateTimes(updatedBooking.PickupDateTime);
                        if (newDateList != null)
                        {
                            // Remove all dates from new date list which already saved in db - for keeping safe from doubling
                            var dlist = lst.Where(o => o.PickupDateTime.Date > newDateList.Max());
                            deleteList.AddRange(dlist);
                            newDateList.RemoveAll(o => lst.Any(x => x.PickupDateTime.Date == o.Date));
                        }

                        if (newRule.Frequency == BookingRule.frequency.Weekly || newRule.Frequency == BookingRule.frequency.Fortnightly)
                        {
                            // change may appear in WEEKDAYS - delete where DAY OFF + add where DAY ON
                            // change may appear in COUNT or UNTIL END DATE - remove any non required from all dates list
                           
                            if (!newRule.Mon)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Monday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Monday);
                            }
                            if (!newRule.Tue)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Tuesday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Tuesday);
                            }
                            if (!newRule.Wed)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Wednesday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Wednesday);
                            }
                            if (!newRule.Thu)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Thursday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Thursday);
                            }
                            if (!newRule.Fri)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Friday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Friday);
                            }
                            if (!newRule.Sat)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Saturday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Saturday);
                            }
                            if (!newRule.Sun)
                            {
                                var d = lst.Where(o => o.PickupDateTime.DayOfWeek == DayOfWeek.Sunday);
                                deleteList.AddRange(d);
                                newDateList?.RemoveAll(o => o.DayOfWeek == DayOfWeek.Sunday);
                            }
                        }
                        else
                        {
                            // NEW FREQUENCY = DAILY 
                            if (oldRule.Frequency == BookingRule.frequency.Weekly)
                            {
                                // + add for all MISSING DAYS ALSO
                                // change may appear in COUNT or UNTIL END DATE  - HANDLED ABOVE 

                                /// ==>> HANDLED IN ABOVE CONDITIONS
                            }
                            if (oldRule.Frequency == BookingRule.frequency.Daily)
                            {
                                // change may appear in COUNT or UNTIL END DATE
                                // if NEW COUNT > OLD COUNT or NEW UNTIL END DATE < OLD UNTIL END DATE --- ADD MORE  
                                // if vice verse - DELETE ANY EXTRA   - - HANDLED ABOVE 

                                /// ==>> HANDLED IN ABOVE CONDITIONS
                            }
                        }

                        // create bookings
                        List<Booking> bookings = new List<Booking>();
                        if (newDateList != null)
                        {
                            foreach (var date in newDateList)
                            {
                                var bookingRepeat = (Booking)updatedBooking.Clone();
                                bookingRepeat.UserId = null;
                                bookingRepeat.SuggestedUserId = null;
                                bookingRepeat.IsASAP = false;
                                bookingRepeat.PaymentReceiptSent = false;
                                bookingRepeat.PaymentLink = string.Empty;
                                bookingRepeat.PaymentOrderId = string.Empty;
                                bookingRepeat.Id = 0;
                                bookingRepeat.PickupDateTime = date;
                                bookingRepeat.RecurrenceID = updatedBooking.RecurrenceID;

                                bookings.Add(bookingRepeat);
                            }
                        }

                        _dB.Bookings.RemoveRange(deleteList);
                        _dB.Bookings.AddRange(bookings);
                        await _dB.SaveChangesAsync();

                        foreach (var item in bookings)
                        {
                            createdList.Add(new CreatedBookingResultDto
                            {
                                BookingId = item.Id,
                                Date = item.PickupDateTime,
                                BookedBy = item.BookedByName,
                                Passenger = item.PassengerName,
                                AccNo = item.AccountNumber.Value
                            });
                        }

                        // now check if we have a return time greater than today and create a new block
                        if (obj.ReturnDateTime.HasValue)
                        {
                            if (obj.ReturnDateTime.Value.Date > DateTime.Now.Date)
                            {
                                var ret = ReverseJourney(bookings.First(),obj.ReturnDateTime.Value);
                                var res1 = await CreateBlockBooking(ret,true);
                                createdList.AddRange(res1.ResultDtos);
                            }
                        }

                    }
                    else // no change in recurrance rule
                    {
                        foreach (var item in lst)
                        {
                            var dt = item.PickupDateTime;
                            var updated = UpdateBookingData(item, obj);

                            if (obj.BookingId == item.Id)
                            {
                                item.UserId = obj.UserId;
                            }
                            else
                            {
                                item.Status = BookingStatus.None;
                            }

                            // -1 t1 is shorter than t2.
                            //  0 t1 is equal to t2.
                            //  1 t1 is longer than t2.

                            if (timeChanged != 0)
                            {
                                updated.PickupDateTime = new DateTime(dt.Year, dt.Month, dt.Day, newTime.Hours, newTime.Minutes, 0);
                            }
                            else
                            {
                                updated.PickupDateTime = dt;
                            }

                            // check if obj has vias and add them
                            if (obj.Vias.Count > 0 && updated.Vias.Count == 0)
                            {
                                foreach (var via in obj.Vias)
                                {
                                    updated.Vias.Add(new BookingVia
                                    {
                                        Address = via.Address,
                                        PostCode = via.PostCode,
                                        ViaSequence = via.ViaSequence,
                                    });
                                }
                            }

                            _dB.Bookings.Update(updated);
                            await _dB.SaveChangesAsync();

                            updated = null;
                        }
                    }

                    await _dB.SaveChangesAsync();
                    return Result.Ok(createdList);
                }
                else
                {
                    return Result.Fail<List<CreatedBookingResultDto>>($"The booking with recurrance id {obj.RecurrenceID} was not found.");
                }
            }
            else // single booking only
            {
                var booking = await _dB.Bookings
                   // .AsNoTracking() -- do not use otherwise we loose audit log
                    .Where(o => o.Id == obj.BookingId)
                    .Include(v=>v.Vias)
                    .FirstOrDefaultAsync();

                if (booking != null)
                {
                    bool changeUserId = false;

                   

                    booking = UpdateBookingData(booking, obj);

                    if (changeUserId)
                    {
                        booking.UserId = obj.UserId;
                    }
                    else
                    {
                        if (obj.UserId == null)
                        {
                            booking.UserId = null;
                        }
                    }

                    // Check if single booking now have any rule for making new repeat bookings. 
                    if (!string.IsNullOrEmpty(booking.RecurrenceRule) && booking.RecurrenceID == null)
                    {
                        var reid = await CreateBlockBooking(booking,true);
                        booking.RecurrenceID = reid.Recurranceid;
                    }

                    // save amendments
                    _dB.Bookings.Update(booking);
                    await _dB.SaveChangesAsync();

                    // check that the driver has changed
                    if (booking.UserId != obj.UserId) // different driver
                    {
                        if (obj.UserId != 0 && obj.UserId != null) // check user id is valid
                        {
                            try
                            {

                                await _dispatchService.AllocateBooking(new AllocateBookingDto
                                {
                                    BookingId = booking.Id,
                                    UserId = obj.UserId.Value,
                                    ActionByUserId = obj.ActionByUserId
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error getting phone number.");
                            }

                            changeUserId = true;
                        }
                    }


                    // send message to driver
                    var date = DateTime.Now.ToUKTime();
                    await _dispatchService.DoJobAmendedMessages(booking, obj.UserId);

                    // check if we have a return date and time and create booking
                    if (obj.ReturnDateTime != null)
                    {
                        var created = await ReturnBooking(date, booking, obj.ReturnDateTime.Value);
                        createdList.AddRange(created);
                    }

                    return Result.Ok(createdList);
                }
                else
                {
                    return Result.Fail<List<CreatedBookingResultDto>>($"The booking with id {obj.BookingId} was not found.");
                }
            }
        }

        //public async Task<Result> UpdateBooking(Booking obj) 
        //{
        //    _dB.Bookings.Update(obj);
        //    await _dB.SaveChangesAsync();
        //    return Result.Ok();
        //}

        /// <summary>
        /// Updates the object data and standardizes the format
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Booking UpdateBookingData(Booking booking, UpdateBookingRequestDto obj)
        {
            // standardize postocodes & address
            booking.PickupPostCode = StandardizePostocde(booking.PickupPostCode);
            booking.PickupAddress = booking.PickupAddress.RemoveExtraSpaces();

            booking.DestinationPostCode = StandardizePostocde(booking.DestinationPostCode);
            booking.DestinationAddress = booking.DestinationAddress.RemoveExtraSpaces();


            if (obj.Vias != null)
            {
                obj.Vias.ForEach(o =>
                {
                    o.Address = o.Address.RemoveExtraSpaces();
                    o.PostCode = StandardizePostocde(o.PostCode);
                });
            }

            if (booking.Vias != null)
            {
                booking.Vias.ForEach(o =>
                {
                    o.Address = o.Address.RemoveExtraSpaces();
                    o.PostCode = StandardizePostocde(o.PostCode);
                });
            }

            booking.PickupAddress = obj.PickupAddress;
            booking.PickupPostCode = obj.PickupPostCode;

            booking.PickupDateTime = obj.PickupDateTime;
            
            booking.PhoneNumber = obj.PhoneNumber;
            booking.ConfirmationStatus = obj.ConfirmationStatus;
            booking.Details = obj.Details;
            booking.Email = obj.Email;
            booking.PhoneNumber = obj.PhoneNumber;
            booking.PassengerName = obj.PassengerName;
            booking.Passengers = obj.Passengers;
            booking.DestinationAddress = obj.DestinationAddress;
            booking.DestinationPostCode = obj.DestinationPostCode;
            booking.IsAllDay = obj.IsAllDay;
            booking.DurationMinutes = obj.DurationMinutes;

            booking.RecurrenceException = obj.RecurrenceException;
            booking.RecurrenceID = obj.RecurrenceID;
            booking.RecurrenceRule = obj.RecurrenceRule;
            booking.Scope = obj.Scope;
            booking.UpdatedByName = obj.UpdatedByName;
            booking.DateUpdated = DateTime.Now.ToUKTime();
            booking.PaymentStatus = obj.PaymentStatus;
            //booking.UserId = obj.UserId;
            
            booking.Price = obj.Price;
            booking.PriceAccount = obj.PriceAccount;
            booking.Mileage = obj.Mileage;
            booking.MileageText = obj.MileageText;
            booking.DurationText = obj.DurationText;
            booking.ActionByUserId = obj.ActionByUserId;
            booking.AccountNumber = obj.AccountNumber;
            booking.ArriveBy = obj.ArriveBy;
            booking.IsASAP = obj.IsASAP;
            booking.ManuallyPriced = obj.ManuallyPriced;

            if (booking.Vias.Any() && obj.Vias.Count == 0 || obj.Vias == null) // need to delete the existing vias
            {
                foreach (var via in booking.Vias)
                {
                    _dB.BookingVias.Remove(via);
                }
            }
            else if(booking.Vias.Count == 0 && obj.Vias.Count > 0) // no previous via but now has
            {
                foreach (var via in obj.Vias)
                {
                    booking.Vias.Add(new BookingVia
                    {
                        Address = via.Address,
                        PostCode = via.PostCode,
                        ViaSequence = via.ViaSequence,
                        Id = 0
                    });
                }
            }
            else // we still have vias - but cant be sure they are the same as the original - delete the existing and add the new
            {
                // remove all
                foreach (var via in booking.Vias)
                {
                    _dB.BookingVias.Remove(via);
                }

                // re-add - clear existing ids
                foreach (var via in obj.Vias)
                {
                    via.Id = 0;
                }
                
                booking.Vias.AddRange(obj.Vias);
            }

            // reset account number if cash or card
            if (booking.Scope == BookingScope.Cash || booking.Scope == BookingScope.Card)
            {
                booking.AccountNumber = 9999;
            }

            //booking.Vias = obj.Vias;

            return booking;
        }

        /// <summary>
        /// Updates the date for a single booking
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Result> UpdateBookingDate(UpdateBookingDateRequest request)
        {
            var res = await _dB.Bookings.Where(o => o.Id == request.BookingId).FirstOrDefaultAsync();
            if (res != null)
            {
                res.ActionByUserId = request.ActionByUserId;
                res.PickupDateTime = request.NewDate;
                res.UpdatedByName = request.UpdatedByName;

                _dB.Bookings.Update(res);

                await _dB.SaveChangesAsync();
                return Domain.Result.Ok();
            }

            return Result.Fail("The booking was not found.");
        }

        public async Task RemoveCancellOnArrival(int bookingId)
        {
            await _dB.Bookings.Where(o => o.Id == bookingId).ExecuteUpdateAsync(o => o.SetProperty(u => u.CancelledOnArrival, false));
        }

        public async Task<Result> CancelBooking(CancelBookingRequest request)
        {
            _logger.LogInformation($"recieved cancel request : [ ID = {request.BookingId} | CancelBlock = {request.CancelBlock} | CancelByName = {request.CancelledByName}");

            var res = await _dB.Bookings.Where(o => o.Id == request.BookingId).FirstOrDefaultAsync();

            await _dB.JobOffers.Where(o => o.BookingId == request.BookingId)
                .ExecuteDeleteAsync();

            if (res != null)
            {
                if (!request.CancelBlock)
                {
                    res.ActionByUserId = request.ActionByUserId;

                    if (request.CancelledOnArrival)
                    {
                        if (res.CancelledOnArrival) // already coa
                        {
                            res.CancelledOnArrival = false; // reverse it
                        }
                        else
                        {
                            res.CancelledOnArrival = true;
                        }
                    }
                    else
                    {
                        res.Cancelled = true;
                        res.CancelledByName = request.CancelledByName;
                    }

                    // check this is not active job in app and if so clear it
                    var cnt = await _dB.DriversOnShift.Where(o => o.ActiveBookingId == request.BookingId).CountAsync();

                    if (cnt > 0)
                    { 
                        await _dispatchService.RemoveActiveJob(request.BookingId);
                    }

                    // send driver and customer messages 
                    await _dispatchService.DoJobCancelledMessages(res,res.UserId);

                    _logger.LogInformation($"cancelling ID {res.Id} for date {res.PickupDateTime}");

                    res.DateUpdated = DateTime.Now.ToUKTime();
                    _dB.Bookings.Update(res);

                    await _dB.SaveChangesAsync();
                    return Domain.Result.Ok();
                }
                else
                {
                    if (res.RecurrenceID != null)
                    {
                        // get date for inital booking
                        var time = await _dB.Bookings.Where(o => o.Id == res.Id).Select(o => o.PickupDateTime).FirstOrDefaultAsync();
                        
                        var bookings = await _dB.Bookings.Where(o => o.RecurrenceID ==
                          res.RecurrenceID && o.PickupDateTime.Date >= time.Date).ToListAsync();

                        if (bookings.Any())
                        {
                            foreach (var booking in bookings)
                            {
                                _logger.LogInformation($"cancelling ID {booking.Id} for date {booking.PickupDateTime}");
                                booking.Cancelled = true;
                                booking.CancelledByName = request.CancelledByName;
                                booking.DateUpdated = DateTime.Now.ToUKTime();

                                _dB.Bookings.Update(booking);
                            }

                            await _dB.SaveChangesAsync();
                        }

                        return Domain.Result.Ok();
                    }
                    else
                    { 
                        return Domain.Result.Fail("No RecurranceID was set."); 
                    }
                }
            }

            return Domain.Result.Fail("The appointment was not found.");
        }

        public async Task<object> GetPickupAddressHistory(string phoneNumber)
        {
            var lst = await _dB.Bookings.Where(o => o.PhoneNumber == phoneNumber).Select(o => new
            {
                Address = o.PickupAddress,
                Postcode = o.PickupPostCode,
                PhoneNo = o.PhoneNumber,
                Name = o.PassengerName,
                EmailAddress = o.Email
            }).ToListAsync();

            lst = lst.DistinctBy(o => o.Address).ToList();

            return lst;
        }

        /// <summary>
        /// Returns a snapshot of the booking data based on the search term.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<KeywordSearchResponseDto> KeywordSearch(string term)
        {
            var res = new KeywordSearchResponseDto();

            var data = await _dB.Bookings.Where(o =>
               (o.PickupAddress.Contains(term) ||
               o.PickupPostCode.StartsWith(term) ||
               o.DestinationAddress.Contains(term) ||
               o.DestinationPostCode.StartsWith(term) ||
               o.Details.Contains(term) ||
               o.PassengerName.Contains(term) ||
               o.PhoneNumber.StartsWith(term) ||
               o.Email.StartsWith(term)) && o.Cancelled == false)
               .AsNoTracking()
               .Select(o => new KeywordSearchResponseDto.ResultItem
               { 
                   BookingId = o.Id,
                   PickupDate = o.PickupDateTime,
                   Pickup = o.PickupAddress,
                   Destination = o.DestinationAddress,
                   DurationMinutes = o.DurationMinutes,
                   Passenger = o.PassengerName,
                   UserId = o.UserId, 
                   Scope = o.Scope
               })
               .Distinct()
               .ToListAsync();

            data = data.Where(o => o.Cancelled == false).ToList();

            // search any vias
            var data2 = await _dB.BookingVias
                .Include(o => o.Booking)
                .Where(o => o.Address
                .StartsWith(term))
                .AsNoTracking()
                .Select(o => o.Booking).Select(o => new KeywordSearchResponseDto.ResultItem
                {
                    BookingId = o.Id,
                    PickupDate = o.PickupDateTime,
                    Pickup = o.PickupAddress,
                    Destination = o.DestinationAddress,
                    DurationMinutes = o.DurationMinutes,
                    Passenger = o.PassengerName,
                    UserId = o.UserId,
                    Scope = o.Scope,
                    Cancelled = o.Cancelled
                })
                .ToListAsync();

            var data3 = data2.Where(o=>o.Cancelled == false).ToList();
            data.AddRange(data3);

            foreach (var item in data)
            {
                if (item.DurationMinutes > 0)
                {
                    item.EndDate = item.PickupDate.AddMinutes(item.DurationMinutes);
                }
            }

            // get distinct users we need colors for
            var usersGrp = data.GroupBy(o => o.UserId).ToList();

            var colors = new Dictionary<int, string>();

            foreach (var user in usersGrp)
            {
                if (user.Key == null)
                    continue;

                if (user.Key != -1 || user.Key != 0)
                {
                    var clr = await _dB.UserProfiles.Include(o => o.User)
                        .Where(o => o.UserId == user.Key)
                        .AsNoTracking().Select(o => o.ColorCodeRGB)
                        .FirstOrDefaultAsync();

                    colors.Add((int)user.Key, clr);
                }
            }

            // assign colors 
            foreach(var b in data) 
            {
                var color = colors.Where(o => o.Key == b.UserId).FirstOrDefault().Value;

                if (b.UserId == null || b.UserId == 0 || b.UserId == -1)
                {
                    b.Color = Constants.UnAllocatedColor;
                }
                else
                {
                    if (!string.IsNullOrEmpty(color))
                    {
                        b.Color = color;
                    }
                }
            }
            
            res.Results = data;

            // get first id nearest today
            if (data.Count > 0)
            {
                var startId = data//.Where(o => o.PickupDate > DateTime.Now.ToUKTime())
                    .OrderBy(o => o.PickupDate)
                    .First()?.BookingId;

                res.Results = res.Results.OrderBy(o => o.PickupDate).ToList();
                res.FocusBookingId = startId;
            }

            return res;
        }

        public async Task<KeywordSearchResponseDto> FindBookings(BookingSearchRequestDto dto)
        {
            var res = new KeywordSearchResponseDto();

            var iquery = _dB.Bookings.AsQueryable();

            if (!string.IsNullOrEmpty(dto.PickupAddress))
            {
                iquery = iquery.Where(o => o.PickupAddress.Contains(dto.PickupAddress));
            }
            if (!string.IsNullOrEmpty(dto.PickupPostcode))
            {
                iquery = iquery.Where(o => o.PickupPostCode.StartsWith(dto.PickupPostcode));
            }
            if (!string.IsNullOrEmpty(dto.DestinationAddress))
            {
                iquery = iquery.Where(o => o.DestinationAddress.Contains(dto.DestinationAddress));
            }
            if (!string.IsNullOrEmpty(dto.DestinationPostcode))
            {
                iquery = iquery.Where(o => o.DestinationPostCode.StartsWith(dto.DestinationPostcode));
            }
            if (!string.IsNullOrEmpty(dto.Passenger))
            {
                iquery = iquery.Where(o => o.PassengerName.Contains(dto.Passenger));
            }
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                iquery = iquery.Where(o => o.PhoneNumber.StartsWith(dto.PhoneNumber));
            }
            if (!string.IsNullOrEmpty(dto.Details))
            {
                iquery = iquery.Where(o => o.Details.Contains(dto.Details));
            }

            iquery = iquery.Where(o => o.Cancelled == false);

            var data = await iquery.AsNoTracking().Select(o => new KeywordSearchResponseDto.ResultItem
            {
                BookingId = o.Id,
                PickupDate = o.PickupDateTime,
                Pickup = o.PickupAddress,
                Destination = o.DestinationAddress,
                DurationMinutes = o.DurationMinutes,
                Passenger = o.PassengerName,
                UserId = o.UserId,
                Scope = o.Scope
            })//.Distinct()
              .ToListAsync();

            data = data.Where(o => o.Cancelled == false).ToList();

            // search any vias
            //var data2 = await _dB.BookingVias
            //    .Include(o => o.Booking)
            //    .Where(o => o.Address
            //    .StartsWith(term))
            //    .AsNoTracking()
            //    .Select(o => o.Booking).Select(o => new KeywordSearchResponseDto.ResultItem
            //    {
            //        BookingId = o.Id,
            //        PickupDate = o.PickupDateTime,
            //        Pickup = o.PickupAddress,
            //        Destination = o.DestinationAddress,
            //        DurationMinutes = o.DurationMinutes,
            //        Passenger = o.PassengerName,
            //        UserId = o.UserId,
            //        Scope = o.Scope,
            //        Cancelled = o.Cancelled
            //    })
            //    .ToListAsync();

            //var data3 = data2.Where(o => o.Cancelled == false).ToList();
            //data.AddRange(data3);

            foreach (var item in data)
            {
                if (item.DurationMinutes > 0)
                {
                    item.EndDate = item.PickupDate.AddMinutes(item.DurationMinutes);
                }
            }

            // get distinct users we need colors for
            var usersGrp = data.GroupBy(o => o.UserId).ToList();

            var colors = new Dictionary<int, string>();

            foreach (var user in usersGrp)
            {
                if (user.Key == null)
                    continue;

                if (user.Key != -1 || user.Key != 0)
                {
                    var clr = await _dB.UserProfiles.Include(o => o.User)
                        .Where(o => o.UserId == user.Key)
                        .AsNoTracking().Select(o => o.ColorCodeRGB)
                        .FirstOrDefaultAsync();

                    colors.Add((int)user.Key, clr);
                }
            }

            // assign colors 
            foreach (var b in data)
            {
                var color = colors.Where(o => o.Key == b.UserId).FirstOrDefault().Value;

                if (b.UserId == null || b.UserId == 0 || b.UserId == -1)
                {
                    b.Color = Constants.UnAllocatedColor;
                }
                else
                {
                    if (!string.IsNullOrEmpty(color))
                    {
                        b.Color = color;
                    }
                }
            }

            res.Results = data;

            // get first id nearest today
            if (data.Count > 0)
            {
                var startId = data//.Where(o => o.PickupDate > DateTime.Now.ToUKTime())
                    .OrderBy(o => o.PickupDate)
                    .First()?.BookingId;

                res.Results = res.Results.OrderBy(o => o.PickupDate).ToList();
                res.FocusBookingId = startId;
            }

            return res;
        }

        public async Task<GetBookingsResponseDto> GetBookingsBySearchTerm(string term)
        {
            var data = await _dB.Bookings.Where(o =>
                o.PickupAddress.Contains(term) || 
                o.PickupPostCode.StartsWith(term) ||
                o.DestinationAddress.Contains(term) ||
                o.DestinationPostCode.StartsWith(term) || 
                o.Details.Contains(term) ||
                o.PassengerName.Contains(term) ||
                o.PhoneNumber.StartsWith(term) ||
                o.Email.StartsWith(term))
                .AsNoTracking()
                .Distinct()
                .ToListAsync();

            // search any vias
            var data2 = await _dB.BookingVias
                .Include(o => o.Booking)
                .Where(o => o.Address
                .StartsWith(term))
                .AsNoTracking()
                .Select(o=>o.Booking)
                .ToListAsync();

            data.AddRange(data2);

            //var repeats = data.Where(o => o.RecurrenceID != null)
            //    .DistinctBy(o => o.RecurrenceID)
            //    .DistinctBy(o => o.PickupAddress)
            //    .DistinctBy(o => o.DestinationAddress)
            //    .ToList();

            //var norepeats = data.Where(o => o.RecurrenceID == null).ToList();
            
            //data.Clear();
            //data.AddRange(repeats);
            //data.AddRange(norepeats);

            var res = new GetBookingsResponseDto();

            foreach (var item in data)
            {
                var color = Constants.UnAllocatedColor;
                var regno = string.Empty;
                var dname = string.Empty;

                if (item.UserId != null)
                {
                    var profile = await _dB.UserProfiles.Include(o => o.User)
                        .Where(o => o.UserId == item.UserId)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    color = profile.ColorCodeRGB;
                    regno = profile.RegNo;
                    dname = profile.User.FullName;
                }

                var model = _mapper.Map<Booking, PersistedBookingModel>(item);
                model.BackgroundColorRGB = color;
                model.Fullname = dname;
                model.RegNo = regno;
                model.BookingId = item.Id;

                res.Bookings.Add(model);
            }

            res.Bookings = res.Bookings.OrderByDescending(o => o.PickupDateTime).ToList();

            return res;
        }

        public async Task<Result<List<(int BookingId, int UserId)>>> SoftAllocateConfirmAll(DateTime date)
        {
            var res = new List<(int BookingId, int UserId)>();

            var soft = await _dB.Bookings
                .Where(o => o.PickupDateTime.Date == date.Date &&
            o.Cancelled == false &&
            o.UserId == null &&
            o.SuggestedUserId != null)
                .Select(o => new { o.Id, o.SuggestedUserId })
                .ToListAsync();

            foreach (var item in soft) 
            {
                await _dB.Bookings.Where(o => o.Id == item.Id)
                   .ExecuteUpdateAsync(o => o.SetProperty(u => u.UserId, item.SuggestedUserId)
                   .SetProperty(u => u.SuggestedUserId, (int?)null));

                res.Add((item.Id, item.SuggestedUserId.Value));
            }

            return Domain.Result.Ok(res);
        }

        public async Task<Result> SoftAllocate(AllocateBookingDto dto)
        {
            await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                .ExecuteUpdateAsync(o => o.SetProperty(u => u.SuggestedUserId, dto.UserId)
                .SetProperty(u => u.ActionByUserId, dto.ActionByUserId));

            return Domain.Result.Ok();
        }

        public async Task<Result> SetPaymentStatus(int bookingId, PaymentStatus status)
        {
               await _dB.Bookings.Where(o => o.Id == bookingId)
                  .ExecuteUpdateAsync(o => o.SetProperty(u => u.PaymentStatus, status));

            return Domain.Result.Ok();
        }

        public async Task<Result> SetPaymentOrderId(int bookingId, string orderid, string paymentLink,string sentByName)
        {
            var date = DateTime.Now.ToUKTime();

            await _dB.Bookings.Where(o => o.Id == bookingId)
               .ExecuteUpdateAsync(o => o.SetProperty(u => u.PaymentOrderId, orderid)
               .SetProperty(u => u.PaymentLink, paymentLink)
               .SetProperty(u=> u.PaymentLinkSentBy,sentByName)
               .SetProperty(u=> u.PaymentLinkSentOn,date));

            return Domain.Result.Ok();
        }

        public async Task<string> GetPaymentOrderId(int bookingId)
        {
            var str = await _dB.Bookings.Where(o => o.Id == bookingId)
                .Select(o => o.PaymentOrderId).FirstOrDefaultAsync();

            return str;
        }

        public async Task<string> GetPaymentLink(int bookingId)
        {
            var str = await _dB.Bookings.Where(o => o.Id == bookingId)
                .Select(o => o.PaymentLink).FirstOrDefaultAsync();

            return str;
        }

        public async Task<Result> SetScope(int bookingId, BookingScope scope)
        {
            await _dB.Bookings.Where(o => o.Id == bookingId)
               .ExecuteUpdateAsync(o => o.SetProperty(u => u.Scope, scope));

            return Domain.Result.Ok();
        }

        public async Task<List<string>> GetPaymentIds()
        {
            var data = await _dB.Bookings.Where(o => o.PickupDateTime >= DateTime.Now &&
                o.Cancelled == false && !string.IsNullOrEmpty(o.PaymentOrderId) && o.PaymentReceiptSent == false)
                .Select(o => o.PaymentOrderId).ToListAsync();

            return data;
        }

        public async Task<Result<int>> UpdatePaymentStatus(string revOrderId)
        {
            var exists = await _dB.Bookings.Where(o => o.PaymentOrderId == revOrderId).CountAsync();
            //var details = await _dB.Bookings.Where(o => o.PaymentOrderId == revOrderId).Select(o=>o.Details).FirstOrDefaultAsync();
            var id = _dB.Bookings.Where(o=>o.PaymentOrderId == revOrderId).Select(o => o.Id).FirstOrDefault();

            if (exists > 0)
            {
                await _dB.Bookings.Where(o => o.PaymentOrderId == revOrderId)
                   .ExecuteUpdateAsync(o => o.SetProperty(u => u.PaymentStatus, PaymentStatus.Paid));
              //     .SetProperty(u => u.Details,details));
            }

            await CreateAndSendPaymentReceipt(id, "Card - Payment Link");

            return Domain.Result.Ok(id);
        }


        public async Task<BookingScope> GetScope(int bookingId)
        {
            return (BookingScope)await _dB.Bookings.Where(o => o.Id == bookingId).Select(o => o.Scope).FirstOrDefaultAsync();
        }

        public async Task CreateAndSendPaymentReceipt(int bookingId, string paymentType)
        {
            await CreateAndSendPaymentReceipt(bookingId, paymentType, "https://ace-server.1soft.co.uk");
        }

        public async Task CreateAndSendPaymentReceipt(int bookingId, string paymentType, string domainUrl)
        {
            var data = await _dB.Bookings.Where(o => o.Id == bookingId)
                .Select(o => 
                    new { 
                        o.PassengerName, 
                        o.Price, 
                        o.PickupAddress,
                        o.PickupPostCode, 
                        o.DestinationAddress,
                        o.DestinationPostCode, 
                        o.PickupDateTime,
                        o.PhoneNumber, 
                        o.Email
                    }).FirstOrDefaultAsync();


            var fname = $"receipt-{bookingId}.pdf";
            var path = $"Data\\Receipts\\{fname}";

            if (!File.Exists(path))
            {
                var detail = $"Taxi journey for {data.PassengerName} from {data.PickupAddress}, {data.PickupPostCode} to {data.DestinationAddress},{data.DestinationPostCode}";

                Settings.License = LicenseType.Community;
                var doc = new PaymentReceipt(data.PassengerName, data.PickupDateTime.ToString("dd/MM/yy HH:mm"), bookingId.ToString(), detail, $"£{data.Price:N2}", paymentType);
                doc.GeneratePdf(path);
            }

            if (!string.IsNullOrEmpty(data.Email))
            {
                // send email receipt
                var bytes = File.ReadAllBytes(path);
                var b64s = Convert.ToBase64String(bytes);

                var res = await _messageService.SendPaymentReceiptEmail(data.Email, data.PassengerName, fname, b64s);

                if (!res) // send text if email fails
                {
                    // send text
                    var url = $"{domainUrl}/api/Bookings/DownloadReceipt?bookingid={bookingId}";
                    var nurl = await _messageService.ShorternUrl(url);

                    if (!string.IsNullOrEmpty(data.PhoneNumber))
                        _messageService.SendPaymentReceiptSMS(data.PhoneNumber, nurl);
                }
            }
            else
            {
                // send text
                var url = $"{domainUrl}/api/Bookings/DownloadReceipt?bookingid={bookingId}";
                var nurl = await _messageService.ShorternUrl(url);

                if(!string.IsNullOrEmpty(data.PhoneNumber))
                    _messageService.SendPaymentReceiptSMS(data.PhoneNumber, nurl);
            }

            await _dB.Bookings.ExecuteUpdateAsync(o => o.SetProperty(u => u.PaymentReceiptSent, true));
        }

        public async Task<List<BookingChangeAudit>> GetAuditLog(string bookingId)
        {
            var data = await _dB.BookingChangeAudits
                .Where(o => o.EntityIdentifier == bookingId)
                .AsNoTracking()
                .ToListAsync();

            data.RemoveAll(o => o.PropertyName == "ActionByUserId" ||
                o.PropertyName == "UpdatedByName");

            data.ForEach(data => 
            { 
                if (data.PropertyName == "UserId") 
                {
                    data.PropertyName = "Allocated Id"; 
                } 
            });

            return data.OrderByDescending(o=>o.TimeStamp).ToList();
        }

        public async Task<List<Booking>> GetCancelledJobs(DateTime date)
        { 
            var data = await _dB.Bookings
                .Where(o=>o.Cancelled == true && o.PickupDateTime.Date == date.Date)
                .Include(o=>o.Vias)
                .AsNoTracking()
                .ToListAsync();

            return data;
        }

        public async Task<List<Booking>> GetActiveCardJobs()
        {
            var data = await _dB.Bookings
                .Include(o => o.Vias)
                .Where(o => o.Cancelled == false && o.Scope == BookingScope.Card &&
                    o.PaymentStatus != PaymentStatus.Paid)// && o.PickupDateTime.Date >= DateTime.Now)
                .AsNoTracking()
                .ToListAsync();

            var a = data.Where(o=>o.Id == 80207).FirstOrDefault();

            return data;
        }

        public async Task RestoreCancelledJob(int bookingId)
        {
            await _dB.Bookings.Where(o => o.Id == bookingId)
                .ExecuteUpdateAsync(b => b
                .SetProperty(u => u.Cancelled, false)
                .SetProperty(u => u.UserId,-1)
                .SetProperty(u => u.Status, BookingStatus.None)
                .SetProperty(u => u.CancelledByName,string.Empty));
        }

        public async Task<List<Booking>> GetUnallocatedJobs(DateTime date)
        {
            var data = await _dB.Bookings
                .Where(o => o.Cancelled == false && o.PickupDateTime.Date == date.Date && o.UserId == null)
                .Include(o => o.Vias)
                .AsNoTracking()
                .ToListAsync();

            return data;
        }

        public async Task<List<Booking>> GetAllocatedJobs(DateTime date)
        {
            var data = await _dB.Bookings
                .Where(o => o.Cancelled == false && o.PickupDateTime.Date == date.Date && o.UserId != null)
                .Include(o => o.Vias)
                .AsNoTracking()
                .ToListAsync();

            return data;
        }

        public async Task<List<Booking>> GetCompletedJobs(DateTime date)
        {
            var data = await _dB.Bookings
                .Where(o => o.Cancelled == false && o.PickupDateTime.Date == date.Date && o.Status == BookingStatus.Complete)
                .Include(o => o.Vias)
                .AsNoTracking()
                .ToListAsync();

            return data;
        }

        public async Task<AirportSearchModel> GetAirportRuns(DateTime from)
        {
            var lst = Constants.Airports;

            var data = await _dB.Bookings
                .Where(o => (o.PickupDateTime.Date >= from.Date && o.PickupDateTime < DateTime.Now) &&
                o.UserId != null && o.Cancelled == false && (lst.Contains(o.PickupAddress) || lst.Contains(o.DestinationAddress))
                ).AsNoTracking()
                .Select(o => new AirportSearchModel.LastTripModel 
                { 
                    Pickup = o.PickupAddress,
                    Destin = o.DestinationAddress,
                    Date = o.PickupDateTime, 
                    Price = (double)o.Price, 
                    UserId = o.UserId.Value
                })
                .ToListAsync();

            var s = data.Where(o=>o.UserId == 8).ToList();

            var data1 = await _dB.UserProfiles
                .Include(o=>o.User)
                .Select(o=> new { o.User.Id, o.User.FullName, o.ColorCodeRGB }).ToListAsync();

            foreach (var entry in data)
            {
                var detail = data1.Where(o => o.Id == entry.UserId).FirstOrDefault();

                entry.Fullname = detail.FullName;
                entry.Color = detail.ColorCodeRGB;
            }

            var res = new AirportSearchModel();
            res.LastAirports = data.OrderBy(o=>o.Date).ToList();

            return res;
        }

        public async Task<List<JourneyCount>> Test() 
        {
            var response = new List<JourneyCount>();
            var bookings = await _dB.Bookings
                        .Include(o => o.Vias)
                        .AsNoTracking()
                        .Where(o => o.PickupDateTime >= DateTime.Now.AddMonths(-1).Date && o.Cancelled == false &&
                        o.PickupDateTime < DateTime.Now && o.Scope == BookingScope.Account).ToListAsync();

            var grpByName = bookings.GroupBy(o => o.PassengerName?.ToLower().Trim());
            foreach (var grp in grpByName)
            {
                var grpByPickup = grp.GroupBy(o => o.PickupAddress);
                foreach (var f in grpByPickup)
                {
                    var timeAM = f.Count(o => o.PickupDateTime.Hour >= 0 && o.PickupDateTime.Hour < 12);
                    var timePM = f.Count(o => o.PickupDateTime.Hour >= 12 && o.PickupDateTime.Hour <= 23);

                    var passengers = grp.Key.Split(new char[] { ',', '-', '&' });
                    foreach (var passenger in passengers) 
                    {
                        response.Add(new JourneyCount() { PassengerName = passenger, Pickup = f.Key, AMCount = timeAM, PMCount = timePM});
                    }
                }
            }
         
            var list = new List<JourneyCount>();
            var grpp = response.GroupBy(o => o.PassengerName?.ToLower().Trim());
            var cultInfo = new CultureInfo("en-GB", false).TextInfo;
            foreach (var gr in grpp) 
            {
                var grpByPickup = gr.GroupBy(o => o.Pickup?.ToLower().Trim());
                foreach (var f in grpByPickup)
                {
                 list.Add(new JourneyCount() { PassengerName = cultInfo.ToTitleCase(gr.Key), Pickup = cultInfo.ToTitleCase(f.Key), AMCount = f.Sum(o => o.AMCount), PMCount = f.Sum(o => o.PMCount) });
                }
            }
            return list.OrderBy(o => o.PassengerName).ToList(); 
        }

        public async Task<(int accno,string email, string bookerName, string passengerName, string pick, string drop)?> GetCancelBookingEmailData(int bookingid)
        {
            var data = await _dB.Bookings
                .Where(o => o.Id == bookingid)
                .Select(o => new { o.AccountNumber, o.PassengerName, 
                    Pickup =$"{o.PickupAddress}, {o.PickupPostCode}", 
                    Dest = $"{o.DestinationAddress}, {o.DestinationPostCode}" })
                .FirstOrDefaultAsync();
            
            if (data != null) 
            {
                var email = await _dB.Accounts.Where(o => o.AccNo == data.AccountNumber)
                    .Select(o => new { o.BookerEmail, o.BookerName})
                    .FirstOrDefaultAsync();

                return (data.AccountNumber.Value, email.BookerEmail, email.BookerName, data.PassengerName, data.Pickup, data.Dest);
            }

            return null;
        }

        public async Task RecordTurnDown(DateTime dateTime, decimal amount)
        {
            await _dB.TurnDowns.AddAsync(new TurnDown { DateTime = dateTime, Amount = amount });
            await _dB.SaveChangesAsync();
        }

        public async Task GetTurnDowns(DateTime from, DateTime to)
        {
            await _dB.TurnDowns.Where(o => o.DateTime >= from && o.DateTime <= to).Select(o => new { o.DateTime, o.Amount }).FirstOrDefaultAsync();
        }

        public async Task<int> CancelBookingsByDateRange(DeleteByRangeRequestDto dto, string name)
        {
            var date = DateTime.Now.ToUKTime();
            var res = await _dB.Bookings
                .Where(o => o.AccountNumber == dto.AccountNo && 
                    (o.PickupDateTime.Date >= dto.From.Date && 
                    o.PickupDateTime <= dto.To.Value.Date.To2359()) && 
                    o.Cancelled == false)
                .ExecuteUpdateAsync(o => 
                    o.SetProperty(u => u.Cancelled, true)
                    .SetProperty(u => u.CancelledByName, name)
                    .SetProperty(u => u.DateUpdated,date));

            return res;
        }

        public async Task<dynamic> CancelBookingsByDateRangeReport(DeleteByRangeRequestDto dto, string name)
        {
            var date = DateTime.Now.ToUKTime();
            var res = await _dB.Bookings
                .Where(o => o.AccountNumber == dto.AccountNo &&
                    (o.PickupDateTime.Date >= dto.From.Date &&
                    o.PickupDateTime <= dto.To.Value.Date.To2359()) &&
                    o.Cancelled == false).Select(o => new { o.Id, o.PickupDateTime, o.PassengerName } ).ToListAsync();
                

            return res;
        }

        public async Task<(bool Error,string Detail)> MergeBookings(int primaryBookingId, int appendBookingId)
        {
            // get primary booking
            var primary = await _dB.Bookings.Include(o=>o.Vias).Where(o => o.Id == primaryBookingId).FirstOrDefaultAsync();

            // get append booking
            var append = await _dB.Bookings.Where(o => o.Id == appendBookingId).FirstOrDefaultAsync();

            // validate
            if (primaryBookingId == appendBookingId)
                return (true, "Incorrect merge, the drop was not executed correctly.");

            if (primary.AccountNumber != append.AccountNumber)
                return (true, "The bookings are on 2 differnt account numbers, you can only merge bookings on the same account.");

            if ((primary.DestinationAddress != append.DestinationAddress ||
                 primary.DestinationPostCode != append.DestinationPostCode) &&
                (primary.PickupAddress != append.PickupAddress ||
                 primary.PickupPostCode != append.PickupPostCode))
            {
                return (true, "Neither the pickup nor the destination addresses match between the bookings.");
            }

            // add pickup as a via
            if (primary.Vias == null)
                primary.Vias = new List<BookingVia>();

            var isPickupSame = primary.PickupPostCode == append.PickupPostCode;

            if (isPickupSame)
            {
                primary.Vias.Add(new BookingVia
                {
                    Address = append.DestinationAddress,
                    PostCode = append.DestinationPostCode,
                    BookingId = primary.Id,
                    ViaSequence = primary.Vias.Count() + 1
                });
            }
            else
            {
                primary.Vias.Add(new BookingVia
                {
                    Address = append.PickupAddress,
                    PostCode = append.PickupPostCode,
                    BookingId = primary.Id,
                    ViaSequence = primary.Vias.Count() + 1
                });
            }

            if (primary.AccountNumber == 9014 || primary.AccountNumber == 10026)
            {
                primary.Price += 7;
                primary.PriceAccount += 15;
            }

            // update passengers
            primary.PassengerName += ", " + append.PassengerName;
            primary.Passengers++;

            // notes
            if (!string.IsNullOrEmpty(append.Details))
            {
                var idx = primary.Vias.Count;
                primary.Details += ($"\r\n[V{idx}] - " + append.Details); 
            }

            primary.ManuallyPriced = true;

            // save and cancel append
            append.Cancelled = true;

            _dB.Bookings.Update(primary);
            _dB.Bookings.Update(append);

            await _dB.SaveChangesAsync();

            return (false, "Bookings Merged");
        }

        public async Task RecordCOAEntry(int accno, DateTime journeyDate, string passengerName, string pickupAddress)
        {
            var data = await _dB.COARecords.Where(o => o.AccountNo == accno && 
                o.COADateTime.Date == DateTime.Now.Date && o.PassengerName == passengerName && o.PickupAddress == pickupAddress)
                .FirstOrDefaultAsync();

            if (data != null) 
            {
                _dB.COARecords.Remove(data);
            }
            else
            {
                await _dB.COARecords.AddAsync(new COARecord
                {
                    AccountNo = accno,
                    COADateTime = DateTime.Now.ToUKTime(),
                    JourneyDateTime = journeyDate,
                    PassengerName = passengerName,
                    PickupAddress = pickupAddress
                });
            }

            await _dB.SaveChangesAsync();
        }

        public async Task<List<COARecord>> GetCOAEntrys(DateTime date)
        {
            var data  = await _dB.COARecords.Where(o => o.COADateTime.Date == date.Date).ToListAsync();
            return data;
        }

        public async Task UpdatePaymentLinkSentDate(int bookingId, string sentByName)
        {
            var date = DateTime.Now.ToUKTime();

            await _dB.Bookings.Where(o => o.Id == bookingId)
               .ExecuteUpdateAsync(o => o
               .SetProperty(u => u.PaymentLinkSentBy, sentByName)
               .SetProperty(u => u.PaymentLinkSentOn, date));
        }

        public async Task FlagVatAddedOnCardAmount(int bookingId, decimal vatAmount, decimal newPrice)
        {
            await _dB.Bookings.Where(o => o.Id == bookingId)
               .ExecuteUpdateAsync(o => o
               .SetProperty(u => u.VatAmountAdded, vatAmount)
               .SetProperty(u => u.Price,newPrice));

            var date = DateTime.Now.ToUKTime();

            await _dB.BookingChangeAudits.AddAsync(new BookingChangeAudit
            {
                EntityIdentifier = bookingId.ToString(),
                UserFullName = "System",
                TimeStamp = date,
                PropertyName = "Price",
                OldValue = (newPrice - vatAmount).ToString(),
                NewValue = newPrice.ToString("N2"),
                Action = "VAT Added on Card Price",
                EntityName = "Booking"
            });

            await _dB.SaveChangesAsync();

            _logger.LogInformation($"Booking {bookingId} flagged with VAT amount {vatAmount} and new price {newPrice}");
        }
    }
}





