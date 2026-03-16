#nullable disable
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Admin;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;

namespace TaxiDispatch.Services;

public class WebBookingService
{
    private readonly TaxiDispatchContext _db;
    private readonly IMapper _mapper;
    private readonly BookingService _bookingService;
    private readonly AceMessagingService _aceMessagingService;
    private readonly UserManager<AppUser> _userManager;
    private readonly TariffService _tariff;
    private readonly UINotificationService _notification;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebBookingService(
        TaxiDispatchContext db,
        IMapper mapper,
        BookingService bookingService,
        AceMessagingService messagingService,
        UserManager<AppUser> userManager,
        TariffService tariff,
        UINotificationService notificationService,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _mapper = mapper;
        _bookingService = bookingService;
        _aceMessagingService = messagingService;
        _userManager = userManager;
        _tariff = tariff;
        _notification = notificationService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WebBookingServiceResult> CreatePolygonAsync(CreateGeoFenceDto dto)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        var cnt = await _db.GeoFences.CountAsync(o => o.Name == dto.Name);
        if (cnt != 0)
        {
            return WebBookingServiceResult.BadRequest("a polygon with the same name already exists");
        }

        var obj = new GeoFence
        {
            Name = dto.Name,
            PolygonData = dto.Data,
            Area = dto.Area,
            Points = dto.Points,
            CreatedOn = DateTime.Now.ToUKTime()
        };

        await _db.GeoFences.AddAsync(obj);
        await _db.SaveChangesAsync();

        return WebBookingServiceResult.Ok(obj);
    }

    public async Task<WebBookingServiceResult> UpdatePolygonAsync(CreateGeoFenceDto dto)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        await _db.GeoFences.Where(o => o.Name == dto.Name).ExecuteDeleteAsync();

        var cnt = await _db.GeoFences.CountAsync(o => o.Name == dto.Name);
        if (cnt != 0)
        {
            return WebBookingServiceResult.BadRequest("a polygon with the same name already exists");
        }

        var obj = new GeoFence
        {
            Name = dto.Name,
            PolygonData = dto.Data,
            Area = dto.Area,
            Points = dto.Points,
            CreatedOn = DateTime.Now.ToUKTime()
        };

        await _db.GeoFences.AddAsync(obj);
        await _db.SaveChangesAsync();

        return WebBookingServiceResult.Ok(obj);
    }

    public async Task<WebBookingServiceResult> DeletePolygonAsync(string polygonName)
    {
        await _db.ZoneToZonePrices
            .Where(o => o.StartZoneName == polygonName || o.EndZoneName == polygonName)
            .ExecuteDeleteAsync();

        await _db.GeoFences
            .Where(o => o.Name == polygonName)
            .ExecuteDeleteAsync();

        return WebBookingServiceResult.Ok();
    }

    public async Task<WebBookingServiceResult> GetAllPolygonsAsync()
    {
        var data = await _db.GeoFences.ToListAsync();
        return WebBookingServiceResult.Ok(data);
    }

    public async Task<WebBookingServiceResult> GetAddressSuggestionsAsync(string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return WebBookingServiceResult.BadRequest("the search string is empty");
        }

        var pois = await _db.LocalPOIs
            .Where(o =>
                (o.Type != LocalPOIType.House && o.Type != LocalPOIType.Airport && o.Type != LocalPOIType.Ferry_Port)
                && o.Address.StartsWith(search) || o.Postcode.StartsWith(search))
            .Select(o => new { o.Address, o.Postcode })
            .ToListAsync();

        return WebBookingServiceResult.Ok(pois);
    }

    public async Task<WebBookingServiceResult> AddNewPassengerAsync(AccountPassengerDto dto)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        var obj = _mapper.Map<AccountPassengerDto, AccountPassenger>(dto);

        await _db.AccountPassengers.AddAsync(obj);
        await _db.SaveChangesAsync();

        return WebBookingServiceResult.Ok(obj);
    }

    public async Task<WebBookingServiceResult> DeletePassengerAsync(int passengerId)
    {
        if (passengerId <= 0)
        {
            return WebBookingServiceResult.BadRequest("the passenger id is invalid");
        }

        var cnt = await _db.AccountPassengers.Where(o => o.Id == passengerId).CountAsync();
        if (cnt != 1)
        {
            return WebBookingServiceResult.NotFound("the passenger id was not found");
        }

        await _db.AccountPassengers
            .Where(o => o.Id == passengerId)
            .ExecuteDeleteAsync();

        return WebBookingServiceResult.Ok(passengerId);
    }

    public async Task<WebBookingServiceResult> GetPassengersAsync(int accountNo)
    {
        if (accountNo == 0)
        {
            return WebBookingServiceResult.BadRequest("Invalid account number.");
        }

        var data = await _db.AccountPassengers
            .Where(o => o.AccNo == accountNo)
            .ToListAsync();

        var res = new List<AccountPassengerDto>();
        foreach (var item in data)
        {
            res.Add(_mapper.Map<AccountPassenger, AccountPassengerDto>(item));
        }

        return WebBookingServiceResult.Ok(res);
    }

    public async Task<WebBookingServiceResult> CreateWebBookingAsync(WebBookingDto dto)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        var job = _mapper.Map<WebBookingDto, WebBooking>(dto);
        job.Passengers = dto.Passengers;
        job.CreatedOn = DateTime.Now.ToUKTime();
        job.Scope = BookingScope.Account;

        await _db.WebBookings.AddAsync(job);
        await _db.SaveChangesAsync();

        await _notification.WebBookingCreated(dto.AccNo);
        await _aceMessagingService.SendBrowserNotification(
            "ACCOUNT BOOKING REQUEST",
            $"Account {dto.AccNo} has requested a booking for {dto.PickupDateTime}.");

        return WebBookingServiceResult.Ok();
    }

    public async Task<WebBookingServiceResult> CreateCashBookingAsync(CashWebBookingDto dto)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        var job = _mapper.Map<CashWebBookingDto, WebBooking>(dto);
        job.DestinationAddress = dto.DestinationAddress;
        job.AccNo = 9999;
        job.CreatedOn = DateTime.Now.ToUKTime();
        job.Scope = dto.Scope.Value;

        job.PickupAddress = dto.PickupAddress
            .Replace(dto.PickupPostCode, "")
            .Replace(",,", ",")
            .Trim()
            .TrimEnd(',');
        job.DestinationAddress = dto.DestinationAddress
            .Replace(dto.DestinationPostCode, "")
            .Replace(",,", ",")
            .Trim()
            .TrimEnd(',');

        job.PickupAddress = WebUtility.UrlDecode(job.PickupAddress);
        job.DestinationAddress = WebUtility.UrlDecode(job.DestinationAddress);

        if (!string.IsNullOrWhiteSpace(job.PassengerName))
        {
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            job.PassengerName = textInfo.ToTitleCase(job.PassengerName.ToLower());
        }

        job.Mileage = dto.Mileage;
        job.MileageText = dto.MileageText;
        job.DurationMinutes = dto.DurationMinutes;
        job.DurationText = dto.DurationText;
        job.Price = dto.Price;
        job.ArriveBy = false;
        job.Luggage = dto.Luggage;
        job.Passengers = dto.Passengers;
        job.Details = WebUtility.UrlDecode(dto.Details);

        await _db.WebBookings.AddAsync(job);
        await _db.SaveChangesAsync();

        await _notification.WebBookingCreated(dto.AccNo);
        await _aceMessagingService.SendBrowserNotification(
            "CASH BOOKING REQUEST",
            $"Account {dto.AccNo} has requested a booking for {dto.PickupDateTime}.");

        _aceMessagingService.SendSmsMessage(
            "07870545494",
            "You have received a new cash booking on ace taxis website, take a look ;-)");
        _aceMessagingService.SendSmsMessage(
            "07572382366",
            "You have received a new cash booking on ace taxis website, take a look ;-)");

        return WebBookingServiceResult.Ok();
    }

    public async Task<WebBookingServiceResult> GetWebBookingsAsync(GetWebBookingsRequestDto dto)
    {
        IQueryable<WebBooking> query;

        if (dto.AccNo.HasValue && dto.AccNo.Value != 0)
        {
            query = _db.WebBookings.Where(o => o.AccNo == dto.AccNo).AsQueryable();
        }
        else
        {
            query = _db.WebBookings.AsQueryable();
        }

        query = query.Where(o => o.Processed == dto.Processed);

        if (dto.Accepted)
        {
            query = query.Where(o => o.Status == WebBookingStatus.Accepted);
        }

        if (dto.Rejected)
        {
            query = query.Where(o => o.Status == WebBookingStatus.Rejected);
        }

        var data = await query.ToListAsync();

        foreach (var item in data)
        {
            if (!string.IsNullOrEmpty(item.RecurrenceRule))
            {
                var rule = new BookingRule(item.RecurrenceRule);
                var str = string.Empty;

                if (rule.Mon) str += "Mon,";
                if (rule.Tue) str += "Tue,";
                if (rule.Wed) str += "Wed,";
                if (rule.Thu) str += "Thu,";
                if (rule.Fri) str += "Fri,";
                if (rule.Sat) str += "Sat,";
                if (rule.Sun) str += "Sun";

                if (rule.UntilEnd.HasValue)
                {
                    var eo = rule.UntilEnd.Value;
                    str += $"\r\nEnds On: {eo:dd/MM/yy}";
                }

                item.RepeatText = str;
            }
        }

        return WebBookingServiceResult.Ok(data);
    }

    public async Task<WebBookingServiceResult> AcceptWebBookingAsync(
        WebBookingAcceptDto dto,
        string username,
        string authHeader,
        CancellationToken cancellationToken)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var obj = await _db.WebBookings.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
            if (obj == null)
            {
                return WebBookingServiceResult.NotFound("the web booking was not found");
            }

            var fullname = await ResolveUserFullNameAsync(username);

            if (!string.IsNullOrEmpty(obj.RecurrenceRule))
            {
                try
                {
                    _ = new BookingRule(obj.RecurrenceRule);
                }
                catch (Exception ex)
                {
                    return WebBookingServiceResult.BadRequest($"The recurrance rule is invalid\r\n{ex.Message}");
                }
            }

            var hh = Convert.ToInt32(dto.RequiredTime.Split(':')[0]);
            var mm = Convert.ToInt32(dto.RequiredTime.Split(':')[1]);
            var dt = new DateTime(
                obj.PickupDateTime.Year,
                obj.PickupDateTime.Month,
                obj.PickupDateTime.Day,
                hh,
                mm,
                0);

            GetPriceResponseDto journey = null;

            if (obj.AccNo != 9999)
            {
                if (obj.AccNo != 9014 && obj.AccNo != 10026)
                {
                    try
                    {
                        journey = await _tariff.GetOnInvoicePrices(
                            obj.PickupPostCode,
                            obj.DestinationPostCode,
                            null,
                            obj.AccNo);
                    }
                    catch
                    {
                        journey = new GetPriceResponseDto();
                    }
                }
                else
                {
                    try
                    {
                        journey = await _tariff.GetPriceHVS(
                            obj.PickupPostCode,
                            obj.DestinationPostCode,
                            null);
                    }
                    catch
                    {
                        journey = new GetPriceResponseDto();
                    }
                }
            }

            var scope = obj.AccNo == 9999 ? obj.Scope : BookingScope.Account;

            var req = new CreateBookingRequestDto
            {
                PickupDateTime = dt,
                PickupAddress = obj.PickupAddress,
                PickupPostCode = obj.PickupPostCode,
                DestinationAddress = obj.DestinationAddress,
                DestinationPostCode = obj.DestinationPostCode,
                PassengerName = obj.PassengerName,
                AccountNumber = obj.AccNo,
                RecurrenceRule = obj.RecurrenceRule,
                Email = obj.Email,
                PhoneNumber = obj.PhoneNumber,
                Scope = scope,
                BookedByName = "[WEB] - " + fullname,
                Passengers = obj.Passengers
            };

            req.DurationMinutes = obj.AccNo == 9999 ? obj.DurationMinutes.Value : journey.TotalMinutes;
            req.Mileage = obj.AccNo == 9999 ? obj.Mileage : (decimal)journey.TotalMileage;
            req.MileageText = obj.AccNo == 9999 ? obj.MileageText : journey.MileageText;
            req.DurationMinutes = obj.AccNo == 9999 ? obj.DurationMinutes.Value : journey.TotalMinutes;
            req.Passengers = obj.Passengers;

            if (dto.Price > 0)
            {
                if (obj.AccNo == 9999)
                {
                    req.Price = (decimal)dto.Price;
                    req.PriceAccount = 0;
                }
                else
                {
                    req.Price = (decimal)dto.Price;
                    req.PriceAccount = (decimal)journey.PriceAccount;
                }
            }
            else
            {
                req.Price = obj.AccNo == 9999 ? (decimal)obj.Price : (decimal)journey.PriceDriver;
                req.PriceAccount = obj.AccNo == 9999 ? 0 : (decimal)journey.PriceAccount;
            }

            if (obj.AccNo == 9999)
            {
                req.Details += $"[Web Booking]\r\n\r\nLuggage: {obj.Luggage}\r\n\r\n{obj.Details}";
                req.Scope = BookingScope.Card;
            }
            else
            {
                req.Details += $"[Web Booking]\r\n\r\n{obj.Details}";
            }

            var created = await _bookingService.CreateBooking(req);
            var createdBookingId = created.First().BookingId;

            var email = string.Empty;
            var name = string.Empty;

            if (obj.AccNo == 9999)
            {
                email = obj.Email;
                name = obj.PassengerName;
            }
            else
            {
                var detail = await _db.Accounts
                    .Where(o => o.AccNo == obj.AccNo)
                    .Select(o => new { o.BookerName, o.BookerEmail })
                    .FirstOrDefaultAsync(cancellationToken);

                email = detail?.BookerEmail ?? string.Empty;
                name = detail?.BookerName ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(email))
            {
                if (obj.AccNo == 9999)
                {
                    await _aceMessagingService.SendCashBookingAcceptedEmail(
                        email,
                        name,
                        new BookingAcceptedEmail
                        {
                            accno = obj.AccNo,
                            pickupaddress = $"{obj.PickupAddress}, {obj.PickupPostCode}",
                            destinationaddress = $"{obj.DestinationAddress}, {obj.DestinationPostCode}",
                            passengername = obj.PassengerName,
                            bookingId = createdBookingId,
                            price = (double)obj.Price,
                            datetime = obj.PickupDateTime.ToString("dd/MM/yy HH:mm")
                        });

                    var url = $"https://ace-server.1soft.co.uk/api/bookings/paymentlink" +
                              $"?bookingId={createdBookingId}" +
                              $"&telephone={Uri.EscapeDataString(obj.PhoneNumber)}" +
                              $"&email={Uri.EscapeDataString(obj.Email)}" +
                              $"&name={Uri.EscapeDataString(obj.PassengerName)}" +
                              $"&price={obj.Price}" +
                              $"&pickup={Uri.EscapeDataString(obj.PickupAddress)}";

                    var client = _httpClientFactory.CreateClient(nameof(WebBookingService));
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);

                    if (!string.IsNullOrEmpty(authHeader)
                        && AuthenticationHeaderValue.TryParse(authHeader, out var parsed))
                    {
                        request.Headers.Authorization = parsed;
                    }

                    using var response = await client.SendAsync(request, cancellationToken);
                    _ = await response.Content.ReadAsStringAsync(cancellationToken);
                }
                else
                {
                    await _aceMessagingService.SendAccountBookingAcceptedEmail(
                        email,
                        name,
                        new BookingAcceptedEmail
                        {
                            accno = obj.AccNo,
                            pickupaddress = $"{obj.PickupAddress}, {obj.PickupPostCode}",
                            destinationaddress = $"{obj.DestinationAddress}, {obj.DestinationPostCode}",
                            passengername = obj.PassengerName,
                            bookingId = createdBookingId,
                            price = dto.Price,
                            datetime = obj.PickupDateTime.ToString("dd/MM/yy HH:mm")
                        });
                }
            }

            obj.Processed = true;
            obj.AcceptedRejectedBy = fullname;
            obj.AcceptedRejectedOn = DateTime.Now.ToUKTime();
            obj.Status = WebBookingStatus.Accepted;

            _db.WebBookings.Update(obj);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return WebBookingServiceResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WebBookingServiceResult> RejectWebBookingAsync(
        WebBookingRejectDto dto,
        string username,
        CancellationToken cancellationToken)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var obj = await _db.WebBookings.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
            if (obj == null)
            {
                return WebBookingServiceResult.NotFound("the web booking was not found");
            }

            var fullname = await ResolveUserFullNameAsync(username);

            var email = string.Empty;
            var name = string.Empty;

            if (obj.AccNo == 9999)
            {
                email = obj.Email;
                name = obj.PassengerName;
            }
            else
            {
                var detail = await _db.Accounts
                    .Where(o => o.AccNo == obj.AccNo)
                    .Select(o => new { o.BookerName, o.BookerEmail })
                    .FirstOrDefaultAsync(cancellationToken);

                email = detail?.BookerEmail ?? string.Empty;
                name = detail?.BookerName ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(email))
            {
                if (obj.AccNo == 9999)
                {
                    await _aceMessagingService.SendCashBookingRejectedEmail(
                        email,
                        name,
                        new BookingRejectedEmail
                        {
                            accno = obj.AccNo,
                            pickupaddress = $"{obj.PickupAddress}, {obj.PickupPostCode}",
                            destinationaddress = $"{obj.DestinationAddress}, {obj.DestinationPostCode}",
                            passengername = obj.PassengerName,
                            reason = dto.Reason,
                            datetime = obj.PickupDateTime.ToString("dd/MM/yy HH:mm")
                        });
                }
                else
                {
                    await _aceMessagingService.SendAccountBookingRejectedEmail(
                        email,
                        name,
                        new BookingRejectedEmail
                        {
                            accno = obj.AccNo,
                            pickupaddress = $"{obj.PickupAddress}, {obj.PickupPostCode}",
                            destinationaddress = $"{obj.DestinationAddress}, {obj.DestinationPostCode}",
                            passengername = obj.PassengerName,
                            reason = dto.Reason,
                            datetime = obj.PickupDateTime.ToString("dd/MM/yy HH:mm")
                        });
                }
            }

            obj.Processed = true;
            obj.AcceptedRejectedBy = fullname;
            obj.AcceptedRejectedOn = DateTime.Now.ToUKTime();
            obj.RejectedReason = UrlEncoder.Create().Encode(dto.Reason);
            obj.Status = WebBookingStatus.Rejected;

            _db.WebBookings.Update(obj);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return WebBookingServiceResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WebBookingServiceResult> AmendAcceptWebBookingAsync(
        WebBookingAmendAcceptDto dto,
        string username,
        CancellationToken cancellationToken)
    {
        if (!dto.IsValid)
        {
            return WebBookingServiceResult.BadRequest("the data is invalid or missing required fields");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var obj = await _db.WebBookings.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
            if (obj == null)
            {
                return WebBookingServiceResult.NotFound("the web booking was not found");
            }

            _ = await ResolveUserFullNameAsync(username);

            obj.PickupDateTime = dto.PickupDateTime;
            obj.Passengers = dto.Passengers;
            obj.Status = WebBookingStatus.Default;
            obj.Processed = false;
            obj.RejectedReason = string.Empty;

            _db.WebBookings.Update(obj);

            if (dto.Vehicles > 1)
            {
                foreach (var _ in Enumerable.Range(1, dto.Vehicles - 1))
                {
                    var newJob = (WebBooking)obj.Clone();
                    newJob.Id = 0;
                    await _db.WebBookings.AddAsync(newJob, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return WebBookingServiceResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WebBookingServiceResult> ShortenUrlAsync(string longUrl)
    {
        if (string.IsNullOrEmpty(longUrl))
        {
            return WebBookingServiceResult.BadRequest("the url is empty");
        }

        var shortUrl = await _aceMessagingService.ShorternUrl(longUrl);
        if (string.IsNullOrEmpty(shortUrl))
        {
            return WebBookingServiceResult.BadRequest("an error occurred while shortening the url");
        }

        return WebBookingServiceResult.Ok(shortUrl);
    }

    public async Task<WebBookingServiceResult> GetDurationAsync(int wid)
    {
        var obj = await _db.WebBookings
            .Where(o => o.Id == wid)
            .Select(o => new
            {
                o.PickupDateTime,
                o.PickupPostCode,
                o.DestinationPostCode
            })
            .FirstOrDefaultAsync();

        if (obj == null)
        {
            return WebBookingServiceResult.NotFound("the web booking was not found");
        }

        var journey = await _tariff.Get9999CashPrice(
            obj.PickupDateTime,
            1,
            obj.PickupPostCode,
            obj.DestinationPostCode,
            null,
            true);

        if (journey == null)
        {
            return WebBookingServiceResult.BadRequest(
                "Possible postcode error, please check they are valid postcodes.");
        }

        return WebBookingServiceResult.Ok(journey.JourneyMinutes + 5);
    }

    public async Task<WebBookingServiceResult> GetAccountActiveBookingsAsync(int accno)
    {
        var active = await _bookingService.GetAccountActiveBookings(accno);
        var lst = active.Select(o => o.BookingId).ToList();

        var pending = await _db.WebAmendmentRequests
            .Where(o => !o.Processed && lst.Contains(o.BookingId))
            .Select(o => new { o.BookingId, o.ApplyToBlock })
            .FirstOrDefaultAsync();

        if (pending != null)
        {
            var pendingBooking = active.FirstOrDefault(o => o.BookingId == pending.BookingId);
            if (pendingBooking != null)
            {
                pendingBooking.ChangesPending = true;

                if (pending.ApplyToBlock)
                {
                    var firstDate = pendingBooking.DateTime;
                    var recurId = pendingBooking.RecurranceId;

                    var block = active
                        .Where(o => o.DateTime.Date > firstDate.Date && o.RecurranceId == recurId)
                        .Select(o => o.BookingId)
                        .ToList();

                    foreach (var item in block)
                    {
                        var blockBooking = active.FirstOrDefault(o => o.BookingId == item);
                        if (blockBooking != null)
                        {
                            blockBooking.ChangesPending = true;
                        }
                    }
                }
            }
        }

        return WebBookingServiceResult.Ok(active);
    }

    public async Task<WebBookingServiceResult> RequestAmendmentAsync(
        int bookingId,
        string message,
        bool block,
        string username)
    {
        var obj = new WebAmendmentRequest
        {
            BookingId = bookingId,
            Amendments = message,
            Processed = false,
            CancelBooking = false,
            ApplyToBlock = block
        };

        await _notification.BookingAmendmentRequest(username, bookingId, message);
        await _db.WebAmendmentRequests.AddAsync(obj);
        await _db.SaveChangesAsync();

        _aceMessagingService.SendSmsMessage(
            "07870545494",
            "You have received a amendment request from an account customer, take a look ;-)");

        return WebBookingServiceResult.Ok();
    }

    public async Task<WebBookingServiceResult> RequestCancellationAsync(
        int bookingId,
        bool block,
        string username)
    {
        var obj = new WebAmendmentRequest
        {
            BookingId = bookingId,
            Amendments = "Cancel Booking",
            Processed = false,
            CancelBooking = true,
            RequestedOn = DateTime.Now.ToUKTime(),
            ApplyToBlock = block
        };

        await _notification.BookingCancelationRequest(username, bookingId);
        await _aceMessagingService.SendBrowserNotification(
            "BOOKING CANCELLATION REQUEST",
            $"Account {username} has requested a cancellation.");
        await _db.WebAmendmentRequests.AddAsync(obj);
        await _db.SaveChangesAsync();

        return WebBookingServiceResult.Ok();
    }

    private async Task<string> ResolveUserFullNameAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return string.Empty;
        }

        var user = await _userManager.FindByNameAsync(username);
        return user?.FullName ?? string.Empty;
    }
}

public sealed class WebBookingServiceResult
{
    public int StatusCode { get; init; }
    public object Value { get; init; }

    public static WebBookingServiceResult Ok(object value = null) =>
        new() { StatusCode = 200, Value = value };

    public static WebBookingServiceResult BadRequest(object value) =>
        new() { StatusCode = 400, Value = value };

    public static WebBookingServiceResult NotFound(object value) =>
        new() { StatusCode = 404, Value = value };
}
