using StackExchange.Redis;

namespace Payments.Api.Infrastructure;

public sealed class IdempotencyCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IdempotencyCache> _logger;

    public IdempotencyCache(IConnectionMultiplexer redis, ILogger<IdempotencyCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<(Guid PaymentId, string RequestHash)?> GetAsync(string key)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(CacheKey(key));
            if (!value.HasValue)
            {
                return null;
            }

            var split = value.ToString().Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                return null;
            }

            if (!Guid.TryParse(split[1], out var paymentId))
            {
                return null;
            }

            return (paymentId, split[0]);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Redis unavailable while reading idempotency key {IdempotencyKey}", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string requestHash, Guid paymentId, TimeSpan ttl)
    {
        try
        {
            var value = $"{requestHash}|{paymentId}";
            await _redis.GetDatabase().StringSetAsync(CacheKey(key), value, ttl, when: When.Always);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Redis unavailable while storing idempotency key {IdempotencyKey}", key);
        }
    }

    private static string CacheKey(string key) => $"idem:payments:{key}";
}
