using System.Text.Json;
using Approva.API.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Approva.API.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _cache.GetStringAsync(key);
        if (value is null) return default;
        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null) return cached;

        var value = await factory();
        if (value is not null)
            await SetAsync(key, value, ttl);
        return value;
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}
