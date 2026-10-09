namespace WebhookPlatform.Application.Common.Interfaces;

/// <summary>
/// Hedef sunucuların istek altında ezilmesini önleyen Rate Limiter arayüzü.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Belirtilen kaynak için limitin aşılıp aşılmadığını kontrol eder ve sayacı artırır.
    /// </summary>
    /// <param name="resourceKey">Limit uygulanacak benzersiz anahtar (Örn: "sub_123").</param>
    /// <param name="limit">Zaman penceresi içindeki maksimum istek sayısı.</param>
    /// <param name="window">Zaman penceresi süresi (Örn: 1 dakika).</param>
    Task<bool> IsAllowedAsync(string resourceKey, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}
