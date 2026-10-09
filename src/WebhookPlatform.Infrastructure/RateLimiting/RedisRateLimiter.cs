using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WebhookPlatform.Application.Common.Interfaces;

namespace WebhookPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Redis tabanlı atomik Token/Fixed-Window Rate Limiting servisi.
/// </summary>
public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimiter> _logger;

    public RedisRateLimiter(IConnectionMultiplexer redis, ILogger<RedisRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(
        string resourceKey, 
        int limit, 
        TimeSpan window, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();

            // Zaman penceresine göre dinamik anahtar üret (Örn: ratelimit:sub_123:202610091615)
            var currentWindowKey = $"ratelimit:{resourceKey}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds}";

            // Atomik olarak sayacı 1 artır
            var count = await db.StringIncrementAsync(currentWindowKey);

            // İlk oluşturulan sayaç ise anahtarın ömrünü (TTL) belirle
            if (count == 1)
            {
                await db.KeyExpireAsync(currentWindowKey, window.Add(TimeSpan.FromSeconds(5)));
            }

            var isAllowed = count <= limit;

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "🚦 Rate Limit aşıldı! Kaynak: {ResourceKey}, Limit: {Limit}, Mevcut İstek: {Count}",
                    resourceKey, limit, count);
            }

            return isAllowed;
        }
        catch (Exception ex)
        {
            // Fail-Open Stratejisi: Redis geçici olarak ulaşılamazsa sistemi durdurma, izin ver ama logla
            _logger.LogError(ex, "⚠️ Redis Rate Limiter hatası. İstek 'Fail-Open' olarak geçişe izin verildi.");
            return true;
        }
    }
}
