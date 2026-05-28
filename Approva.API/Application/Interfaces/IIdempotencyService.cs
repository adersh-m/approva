namespace Approva.API.Application.Interfaces;

public interface IIdempotencyService
{
    Task<string?> GetResultAsync(string key);
    Task StoreResultAsync(string key, string result, TimeSpan ttl);
}
