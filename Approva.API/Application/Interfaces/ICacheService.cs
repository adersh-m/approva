namespace ExpenseApp.API.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    Task RemoveAsync(string key);
}
