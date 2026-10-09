namespace WebhookPlatform.Domain.Enums;

/// <summary>
/// Webhook teslimat denemesinin durumunu temsil eder.
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Bildirim sıraya alındı, gönderilmeyi bekliyor.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Gönderim şu an aktif olarak işleniyor / HTTP çağrısı yapılıyor.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Hedef sunucu başarılı HTTP 2xx yanıtı döndü.
    /// </summary>
    Success = 3,

    /// <summary>
    /// Hedef sunucu hata döndü veya zaman aşımına uğradı, tekrar denenecek.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Maksimum yeniden deneme sayısı aşıldı, Dead-Letter Queue (DLQ)'ya alındı.
    /// </summary>
    DeadLettered = 5
}
