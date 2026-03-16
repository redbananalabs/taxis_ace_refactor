using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services
{
    public class GeoZoneService : BaseService<GeoZoneService>
    {
        // Add this field if not already present
        private readonly ILogger<GeoZoneService> _logger;

        public GeoZoneService(IDbContextFactory<TaxiDispatchContext> factory, ILogger<GeoZoneService> logger) : base(factory, logger)
        {
            _logger = logger;
        }

        public async Task<ZoneToZonePrice> AddZonePrice(ZoneToZonePrice dto)
        {
            await _dB.ZoneToZonePrices.AddAsync(dto);
            await _dB.SaveChangesAsync();

            return dto;
        }

        public async Task UpdateZonePrice(ZoneToZonePrice dto)
        {
            await _dB.ZoneToZonePrices.Where(o => o.Id == dto.Id)
                    .ExecuteUpdateAsync(o => o.SetProperty(u => u.StartZoneName, dto.StartZoneName)
                    .SetProperty(u => u.EndZoneName, dto.EndZoneName)
                    .SetProperty(u => u.Cost, dto.Cost)
                    .SetProperty(u => u.Charge, dto.Charge));
        }

        public async Task<List<ZoneToZonePrice>> GetZonePrices()
        {
            return await _dB.ZoneToZonePrices.AsNoTracking().ToListAsync();
        }
    }
}

