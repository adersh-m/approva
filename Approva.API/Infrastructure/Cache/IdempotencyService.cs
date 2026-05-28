using Approva.API.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Approva.API.Infrastructure.Cache;

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;

    public IdempotencyService(IDistributedCache cache) => _cache = cache;

    public Task<string?> GetResultAsync(string key) => _cache.GetStringAsync(key);

    public Task StoreResultAsync(string key, string result, TimeSpan ttl) =>
        _cache.SetStringAsync(key, result, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
}
