using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PusherServer;
using TaxiDispatch.Configuration;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;

namespace TaxiDispatch.Services;

public sealed class CallEventsService
{
    private readonly TaxiDispatchContext _db;
    private readonly CallEventsConfig _config;
    private readonly ILogger<CallEventsService> _logger;

    public CallEventsService(
        TaxiDispatchContext db,
        IOptions<CallEventsConfig> options,
        ILogger<CallEventsService> logger)
    {
        _db = db;
        _logger = logger;
        _config = options.Value;
    }

    public async Task<CallNotificationPayload> PublishCallNotificationAsync(
        string callerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now.ToUKTime();
        var payload = await BuildPayloadAsync(
            callerId,
            now,
            useDateOnlyForCancelledFilter: true,
            setNullAccountToFallback: false,
            cancellationToken);

        var pusher = CreatePusherClient();
        var json = JsonConvert.SerializeObject(payload);
        await pusher.TriggerAsync(_config.Channel, _config.EventName, new { message = json });

        return payload;
    }

    public Task<CallNotificationPayload> BuildCallerLookupAsync(
        string callerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now.ToUKTime();
        return BuildPayloadAsync(
            callerId,
            now,
            useDateOnlyForCancelledFilter: false,
            setNullAccountToFallback: true,
            cancellationToken);
    }

    private async Task<CallNotificationPayload> BuildPayloadAsync(
        string callerId,
        DateTime referenceTime,
        bool useDateOnlyForCancelledFilter,
        bool setNullAccountToFallback,
        CancellationToken cancellationToken)
    {
        var query = @" SELECT * FROM (SELECT *, ROW_NUMBER() OVER (PARTITION BY PickupAddress ORDER BY PickupDateTime DESC) AS RowNum
                            FROM Bookings 
                            WHERE PhoneNumber = {0} AND PickupDateTime < {1}) AS DistinctBookings
                            WHERE RowNum = 1
                            ORDER BY PickupDateTime DESC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY ";

        var previous = await _db.Bookings
            .FromSqlRaw(query, callerId, referenceTime)
            .AsNoTracking()
            .Select(o => new CallHistoryEntry
            {
                PickupDateTime = o.PickupDateTime,
                PickupAddress = o.PickupAddress,
                PickupPostCode = o.PickupPostCode,
                DestinationAddress = o.DestinationAddress,
                DestinationPostCode = o.DestinationPostCode,
                Details = o.Details,
                PassengerName = o.PassengerName,
                PhoneNumber = o.PhoneNumber,
                Email = o.Email,
                ChargeFromBase = o.ChargeFromBase,
                Price = o.Price,
                DurationMinutes = o.DurationMinutes,
                Scope = BookingScope.Cash,
                AccountNumber = setNullAccountToFallback
                    ? o.AccountNumber ?? 9999
                    : o.AccountNumber,
                Cancelled = o.Cancelled
            })
            .ToListAsync(cancellationToken);

        var current = await _db.Bookings
            .Where(o => o.PhoneNumber == callerId &&
                        o.PickupDateTime >= referenceTime &&
                        o.Cancelled == false)
            .Take(10)
            .AsNoTracking()
            .OrderBy(o => o.PickupDateTime)
            .Include(o => o.Vias)
            .ToListAsync(cancellationToken);

        foreach (var item in current)
        {
            item.UserId = null;
            item.Status = BookingStatus.None;
            item.BookedByName = string.Empty;
        }

        var cancelledQuery = _db.Bookings
            .Where(o => o.PhoneNumber == callerId && o.Cancelled == true);

        cancelledQuery = useDateOnlyForCancelledFilter
            ? cancelledQuery.Where(o => o.PickupDateTime.Date >= referenceTime)
            : cancelledQuery.Where(o => o.PickupDateTime >= referenceTime);

        var cancelled = await cancelledQuery
            .Take(3)
            .AsNoTracking()
            .OrderBy(o => o.PickupDateTime)
            .Include(o => o.Vias)
            .ToListAsync(cancellationToken);

        foreach (var c in cancelled)
        {
            previous.Add(new CallHistoryEntry
            {
                PickupDateTime = c.PickupDateTime,
                PickupAddress = c.PickupAddress,
                PickupPostCode = c.PickupPostCode,
                DestinationAddress = c.DestinationAddress,
                DestinationPostCode = c.DestinationPostCode,
                Details = c.Details,
                PassengerName = c.PassengerName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                ChargeFromBase = c.ChargeFromBase,
                Price = c.Price,
                DurationMinutes = c.DurationMinutes,
                Scope = BookingScope.Cash,
                AccountNumber = setNullAccountToFallback
                    ? c.AccountNumber ?? 9999
                    : c.AccountNumber,
                Cancelled = c.Cancelled
            });
        }

        return new CallNotificationPayload
        {
            Current = current
                .DistinctBy(o => o.PickupAddress)
                .OrderByDescending(o => o.PickupDateTime)
                .ToList(),
            Previous = previous
                .DistinctBy(o => o.PickupAddress)
                .OrderByDescending(o => o.PickupDateTime)
                .ToList(),
            Telephone = NormalizeTelephone(callerId)
        };
    }

    private Pusher CreatePusherClient()
    {
        if (string.IsNullOrWhiteSpace(_config.AppId) ||
            string.IsNullOrWhiteSpace(_config.Key) ||
            string.IsNullOrWhiteSpace(_config.Secret))
        {
            throw new InvalidOperationException("CallEvents Pusher configuration is missing.");
        }

        var options = new PusherOptions
        {
            Cluster = _config.Cluster,
            Encrypted = true
        };

        return new Pusher(_config.AppId, _config.Key, _config.Secret, options);
    }

    private static string NormalizeTelephone(string callerId)
    {
        if (callerId.StartsWith("044"))
        {
            return "0" + callerId[2..];
        }

        if (callerId.StartsWith("+44"))
        {
            return "0" + callerId[2..];
        }

        return callerId;
    }
}

public sealed class CallNotificationPayload
{
    public List<Booking> Current { get; set; } = new();

    public List<CallHistoryEntry> Previous { get; set; } = new();

    public string Telephone { get; set; } = string.Empty;
}

public sealed class CallHistoryEntry
{
    public DateTime PickupDateTime { get; set; }

    public string PickupAddress { get; set; } = string.Empty;

    public string PickupPostCode { get; set; } = string.Empty;

    public string DestinationAddress { get; set; } = string.Empty;

    public string DestinationPostCode { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string? PassengerName { get; set; }

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool ChargeFromBase { get; set; }

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public BookingScope Scope { get; set; }

    public int? AccountNumber { get; set; }

    public bool Cancelled { get; set; }
}
