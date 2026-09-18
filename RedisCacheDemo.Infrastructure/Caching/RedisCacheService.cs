using RedisCacheDemo.Application.Abstractions.Caching;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisCacheDemo.Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var database = _connectionMultiplexer.GetDatabase();

            var value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task RemoveAsync(string key)
        {
            var database = _connectionMultiplexer.GetDatabase();

            await database.KeyDeleteAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var database = _connectionMultiplexer.GetDatabase();

            var serializedValue = JsonSerializer.Serialize(value);

            if (expiration.HasValue)
            {
                await database.StringSetAsync(
                    key,
                    serializedValue,
                    new Expiration(expiration.Value));

                return;
            }

            await database.StringSetAsync(
                key,
                serializedValue);
        }
    }
}
