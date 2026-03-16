using Microsoft.EntityFrameworkCore;
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;

namespace TaxiDispatch.Services;

public sealed class UrlTrackingService
{
    private readonly TaxiDispatchContext _db;

    public UrlTrackingService(TaxiDispatchContext db)
    {
        _db = db;
    }

    public async Task<string?> ResolveAndTrackLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var mapping = await _db.UrlMappings
            .FirstOrDefaultAsync(m => m.ShortCode == shortCode, cancellationToken);

        if (mapping == null)
        {
            return null;
        }

        mapping.Clicks++;
        _db.UrlMappings.Update(mapping);
        await _db.SaveChangesAsync(cancellationToken);

        return mapping.LongUrl;
    }

    public async Task TrackQrCodeClickAsync(string location, CancellationToken cancellationToken = default)
    {
        await _db.QRCodeClicks.AddAsync(new QRCodeClick
        {
            Location = location,
            TimeStamp = DateTime.Now.ToUKTime()
        }, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
