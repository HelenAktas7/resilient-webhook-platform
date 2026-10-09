using WebhookPlatform.Application.Common.Messages;

namespace WebhookPlatform.Application.Common.Interfaces;

public record DeliveryExecutionResult(
    bool IsSuccess,
    int? HttpStatusCode,
    long ResponseTimeMs,
    string? ResponseBody,
    string? ErrorMessage
);

/// <summary>
/// Webhook mesajlarını hedef sunucuya HTTP ve Polly dayanıklılık motoru ile ileten servis arayüzü.
/// </summary>
public interface IWebhookDeliveryService
{
    Task<DeliveryExecutionResult> DeliverAsync(WebhookDispatchMessage message, CancellationToken cancellationToken = default);
}
