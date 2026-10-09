namespace WebhookPlatform.Application.Common.Messages;

/// <summary>
/// RabbitMQ kuyruğuna fırlatılan ve teslim edilmeyi bekleyen Webhook görev mesajı.
/// </summary>
public record WebhookDispatchMessage
{
    public Guid EventId { get; init; }
    public Guid SubscriptionId { get; init; }
    public string TargetUrl { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public int AttemptNumber { get; init; } = 1;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
