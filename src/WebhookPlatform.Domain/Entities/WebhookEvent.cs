namespace WebhookPlatform.Domain.Entities;

/// <summary>
/// Sisteme gelen ve dağıtılmayı bekleyen orijinal olay kaydı.
/// </summary>
public class WebhookEvent
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Aynı olayın iki kez işlenmesini önlemek için benzersiz anahtar (Idempotency).
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>
    /// Olayın türü (Örn: "order.created", "payment.succeeded").
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Taşınan JSON yükü (Payload).
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    // EF Core için parametresiz constructor
    private WebhookEvent() { }

    public WebhookEvent(string idempotencyKey, string eventType, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey boş olamaz.", nameof(idempotencyKey));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType boş olamaz.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new ArgumentException("PayloadJson boş olamaz.", nameof(payloadJson));

        Id = Guid.NewGuid();
        IdempotencyKey = idempotencyKey;
        EventType = eventType;
        PayloadJson = payloadJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
