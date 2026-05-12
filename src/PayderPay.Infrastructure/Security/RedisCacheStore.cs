using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayderPay.Application.Common.Interfaces.Security;
using PayderPay.Application.Common.Settings;

namespace PayderPay.Infrastructure.Security;

public class RedisCacheStore : IRedisCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _distributedCache;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCacheStore> _logger;

    public RedisCacheStore(
        IDistributedCache distributedCache,
        IOptions<RedisSettings> settings,
        ILogger<RedisCacheStore> logger)
    {
        _distributedCache = distributedCache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return default;
        }

        try
        {
            var value = await _distributedCache.GetStringAsync(BuildKey(key), cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key '{CacheKey}'. Continuing without cache.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || ttl <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(value, JsonOptions);
            await _distributedCache.SetStringAsync(
                BuildKey(key),
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key '{CacheKey}'. Continuing without cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        try
        {
            await _distributedCache.RemoveAsync(BuildKey(key), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key '{CacheKey}'. Continuing without cache.", key);
        }
    }

    private string BuildKey(string key)
    {
        if (string.IsNullOrWhiteSpace(_settings.KeyPrefix))
        {
            return key;
        }

        return $"{_settings.KeyPrefix}:{key}";
    }
}
