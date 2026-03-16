using TaxiDispatch.Data;
using TaxiDispatch.DTOs._Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Services.Cache
{
    public sealed class StartupCacheService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartupCacheService> _logger;

        public StartupCacheService(
            IServiceScopeFactory scopeFactory,
            ILogger<StartupCacheService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await FillGpsLocations(cancellationToken);
            await FillUserProfiles(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;


        private async Task FillGpsLocations(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting GPS Location cache warm-up");
            var date = DateTime.Now.ToUKTime().AddMinutes(-5);

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<TaxiDispatchContext>();
                var cache = scope.ServiceProvider.GetRequiredService<UserLocationCacheService>();

                // Load last known GPS per driver 
                var lastLocations = await db.DriverLocationHistories
                    .AsNoTracking()
                    .Where(o => o.TimeStamp >= date)
                    .GroupBy(x => x.UserId)
                    .Select(g => g
                        .OrderByDescending(x => x.TimeStamp)
                        .First())
                    .ToListAsync(cancellationToken);

                foreach (var loc in lastLocations)
                {
                    await cache.SetAsync(new CachedLocation
                    {
                        UserId = loc.UserId,
                        Latitude = loc.Latitude.Value == null ? 0 : loc.Latitude.Value,
                        Longitude = loc.Longitude.Value == null ? 0 : loc.Longitude.Value,
                        Speed = loc.Speed.Value == null ? 0 : loc.Speed.Value,
                        Heading = loc.Heading == null ? 0 : loc.Heading.Value,
                        Timestamp = loc.TimeStamp
                    });
                }

                _logger.LogInformation(
                    "GPS Location cache warm-up complete ({Count} drivers)",
                    lastLocations.Count);
            }
            catch (Exception ex)
            {
                // IMPORTANT: do NOT crash startup
                _logger.LogError(ex, "GPS Location cache warm-up failed");
            }
        }

        private async Task FillUserProfiles(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting User Profile cache warm-up");

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<TaxiDispatchContext>();
                var cache = scope.ServiceProvider.GetRequiredService<UserProfileCacheService>();
                var profile = scope.ServiceProvider.GetRequiredService<UserProfileService>();

                var data = await profile.ListUsers();

                foreach (var user in data.Users)
                {
                    await cache.SetAsync(new CachedUserProfile
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        FullName = user.FullName,
                        Registation = user.RegNo, 
                        HtmlColorCode = user.ColorRGB,
                        Make = user.VehicleMake,
                        Model = user.VehicleModel
                    });
                }

                _logger.LogInformation(
                   "User Profile cache warm-up complete ({Count} drivers)",
                   data.Users.Count);
            }
            catch (Exception ex)
            {
                // IMPORTANT: do NOT crash startup
                _logger.LogError(ex, "User Profile cache warm-up failed");
            }
        }
    }
}


