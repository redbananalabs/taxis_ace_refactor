using TaxiDispatch.DTOs._Cache;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace TaxiDispatch.Services.Cache
{
    public class UserProfileCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IDatabase _redisDb;

        public UserProfileCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redisDb = redis.GetDatabase();
        }

        private static string UserProfileKey(int userId)
        => $"driver:userprofile:{userId}";

        public async Task SetAsync(CachedUserProfile profile)
        {
            var json = JsonSerializer.Serialize(profile);

            //await _cache.SetStringAsync(
            //    UserProfileKey(profile.UserId),
            //    json,
            //    new DistributedCacheEntryOptions
            //    {
            //        AbsoluteExpirationRelativeToNow =
            //            TimeSpan.FromDays(365)
            //    });

            await _redisDb.StringSetAsync(UserProfileKey(profile.UserId), json);
        }

        public async Task<CachedUserProfile?> GetAsync(int userId)
        {
            var json = await _redisDb.StringGetAsync(UserProfileKey(userId));

            //var json = await _cache.GetStringAsync(UserProfileKey(userId));
            //return json == null
                //? null
                //: JsonSerializer.Deserialize<CachedUserProfile>(json);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<CachedUserProfile>(json);
        }
    }
}

