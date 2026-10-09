using WebhookPlatform.Domain.Enums;

namespace WebhookPlatform.Domain.Entities;

/// <summary>
/// Bir Webhook olayının belirli bir aboneye iletilmesi için yapılan her bir HTTP denemesinin kaydı.
/// </summary>
public class WebhookDeliveryAttempt
{
    public Guid Id { get; private set; }

    public Guid WebhookEventId { get; private set; }
    public Guid WebhookSubscriptionId { get; private set; }

    /// <summary>
    /// Deneme numarası (1, 2, 3...).
    /// </summary>
    public int AttemptNumber { get; private set; }

    /// <summary>
    /// Denemenin güncel durumu.
    /// </summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>
    /// Hedef sunucudan dönen HTTP durum kodu (200, 500 vb. - Zaman aşımında null olabilir).
    /// </summary>
    public int? HttpStatusCode { get; private set; }

    /// <summary>
    /// İsteğin yanıtlanma süresi (milisaniye cinsinden).
    /// </summary>
    public long ResponseTimeMs { get; private set; }

    /// <summary>
    /// Hedef sunucudan dönen yanıt gövdesi (Örn: Hata mesajları veya onay JSON'ı).
    /// </summary>
    public string? ResponseBody { get; private set; }

    /// <summary>
    /// Ağ hatası veya istisna (Exception) durumunda oluşan hata mesajı.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    public DateTime AttemptedAtUtc { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }

    // Navigation properties (EF Core)
    public WebhookEvent? WebhookEvent { get; private set; }
    public WebhookSubscription? WebhookSubscription { get; private set; }

    // EF Core için parametresiz constructor
    private WebhookDeliveryAttempt() { }

    public WebhookDeliveryAttempt(
        Guid webhookEventId, 
        Guid webhookSubscriptionId, 
        int attemptNumber)
    {
        Id = Guid.NewGuid();
        WebhookEventId = webhookEventId;
        WebhookSubscriptionId = webhookSubscriptionId;
        AttemptNumber = attemptNumber;
        Status = DeliveryStatus.InProgress;
        AttemptedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsSucceeded(int httpStatusCode, long responseTimeMs, string? responseBody)
    {
        Status = DeliveryStatus.Success;
        HttpStatusCode = httpStatusCode;
        ResponseTimeMs = responseTimeMs;
        ResponseBody = responseBody?.Length > 2000 ? responseBody[..2000] : responseBody;
        ErrorMessage = null;
        NextRetryAtUtc = null;
    }

    public void MarkAsFailed(int? httpStatusCode, long responseTimeMs, string? errorMessage, DateTime? nextRetryAtUtc)
    {
        Status = DeliveryStatus.Failed;
        HttpStatusCode = httpStatusCode;
        ResponseTimeMs = responseTimeMs;
        ErrorMessage = errorMessage?.Length > 2000 ? errorMessage[..2000] : errorMessage;
        NextRetryAtUtc = nextRetryAtUtc;
    }

    public void MarkAsDeadLettered(int? httpStatusCode, long responseTimeMs, string? errorMessage)
    {
        Status = DeliveryStatus.DeadLettered;
        HttpStatusCode = httpStatusCode;
        ResponseTimeMs = responseTimeMs;
        ErrorMessage = errorMessage?.Length > 2000 ? errorMessage[..2000] : errorMessage;
        NextRetryAtUtc = null;
    }
}
