using StackExchange.Redis;
using System.Text.Json;

namespace Convene.Infrastructure.Services
{
    public interface IRedisService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);

        Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry);
        Task ReleaseLockAsync(string key);

        Task ReleaseLockAsync(string key, string value);
    }

    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (_redis == null) return; // Redis unavailable, skip
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry, When.Always, CommandFlags.None);
        }


        public async Task<T> GetAsync<T>(string key)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task RemoveAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }


        public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry)
        {
            var db = _redis.GetDatabase();
            return await db.StringSetAsync(key, value, expiry, When.NotExists);
        }

        public async Task ReleaseLockAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }


        // overlodading for two argument method
        public async Task ReleaseLockAsync(string key, string value)
        {
            var db = _redis.GetDatabase();

            string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

            await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { value });
        }
    }
}
