using TaxiDispatch.DTOs._Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace TaxiDispatch.Services.Cache
{
    public class UserLocationCacheService
    {
        private readonly ILogger<UserLocationCacheService> _logger;
        private readonly ConfigurationOptions _redisOptions;

        private static ConnectionMultiplexer? _mux;
        private static readonly SemaphoreSlim _reconnectLock = new(1, 1);

        private const string ActiveDriversKey = "driver:active";

        public UserLocationCacheService(
            ILogger<UserLocationCacheService> logger,
            IConfiguration config)
        {
            _logger = logger;

            _redisOptions = ConfigurationOptions.Parse(
                config["Redis:ConnectionString"]);

            _redisOptions.AbortOnConnectFail = false;
            _redisOptions.ConnectRetry = 5;
            _redisOptions.ConnectTimeout = 5000;
            _redisOptions.SyncTimeout = 5000;
            _redisOptions.KeepAlive = 30;
            _redisOptions.ReconnectRetryPolicy = new ExponentialRetry(5000);
        }

        private static string LocationKey(int userId)
            => $"driver:location:{userId}";

        // ------------------------------------------------------
        // PUBLIC API
        // ------------------------------------------------------

        public async Task SetAsync(CachedLocation location)
        {
            try
            {
                var db = await GetDatabaseAsync();

                var key = LocationKey(location.UserId);

                var json = JsonSerializer.Serialize(location);

                // Store location
                await db.StringSetAsync(
                    key,
                    json,
                    expiry: TimeSpan.FromMinutes(2));

                // Track active drivers
                await db.SetAddAsync(ActiveDriversKey, location.UserId);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable – GPS cache write skipped");
                throw;
            }
        }

        public async Task<CachedLocation?> GetAsync(int userId)
        {
            try
            {
                var db = await GetDatabaseAsync();

                var json = await db.StringGetAsync(LocationKey(userId));

                return json.IsNullOrEmpty
                    ? null
                    : JsonSerializer.Deserialize<CachedLocation>(json!);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable – GPS cache read skipped");
                return null;
            }
        }

        public async Task<IReadOnlyList<CachedLocation>> GetAllAsync()
        {
            var result = new List<CachedLocation>();

            try
            {
                var db = await GetDatabaseAsync();

                var userIds = await db.SetMembersAsync(ActiveDriversKey);

                foreach (var id in userIds)
                {
                    var key = LocationKey((int)id);

                    var json = await db.StringGetAsync(key);

                    if (json.IsNullOrEmpty)
                    {
                        // Expired → clean index
                        await db.SetRemoveAsync(ActiveDriversKey, id);
                        continue;
                    }

                    var location = JsonSerializer.Deserialize<CachedLocation>(json!);
                    if (location != null)
                        result.Add(location);
                }
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable – returning empty driver list");
            }

            return result;
        }

        // ------------------------------------------------------
        // INTERNAL – SELF-HEALING REDIS ACCESS
        // ------------------------------------------------------

        private async Task<IDatabase> GetDatabaseAsync()
        {
            if (_mux is { IsConnected: true })
                return _mux.GetDatabase();

            await _reconnectLock.WaitAsync();
            try
            {
                if (_mux is { IsConnected: true })
                    return _mux.GetDatabase();

                _logger.LogWarning("Redis disconnected – recreating multiplexer");

                _mux?.Dispose();
                _mux = await ConnectionMultiplexer.ConnectAsync(_redisOptions);
            }
            finally
            {
                _reconnectLock.Release();
            }

            return _mux.GetDatabase();
        }
    }
}

