using MassTransit;
using Microsoft.Extensions.Logging;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Application.Common.Messages;
using WebhookPlatform.Domain.Entities;

namespace WebhookPlatform.Worker.Consumers;

/// <summary>
/// RabbitMQ'dan gelen WebhookDispatchMessage görevlerini tüketen ve teslimat loglarını kaydeden Consumer.
/// </summary>
public class WebhookDispatchConsumer : IConsumer<WebhookDispatchMessage>
{
    private readonly IWebhookDeliveryService _deliveryService;
    private readonly IWebhookDbContext _dbContext;
    private readonly ILogger<WebhookDispatchConsumer> _logger;

    public WebhookDispatchConsumer(
        IWebhookDeliveryService deliveryService,
        IWebhookDbContext dbContext,
        ILogger<WebhookDispatchConsumer> logger)
    {
        _deliveryService = deliveryService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WebhookDispatchMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📨 Yeni webhook iletim görevi alındı. EventId: {EventId}, Hedef: {TargetUrl}, Deneme: {AttemptNumber}",
            message.EventId,
            message.TargetUrl,
            message.AttemptNumber);

        // 1. Teslimat Deneme Kaydını Başlat
        var attempt = new WebhookDeliveryAttempt(
            message.EventId,
            message.SubscriptionId,
            message.AttemptNumber
        );

        _dbContext.DeliveryAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        // 2. Polly Donanımlı İletim Servisini Çağır
        var result = await _deliveryService.DeliverAsync(message, context.CancellationToken);

        // 3. Denemenin Sonucunu Veritabanına Güncelle
        if (result.IsSuccess)
        {
            attempt.MarkAsSucceeded(result.HttpStatusCode ?? 200, result.ResponseTimeMs, result.ResponseBody);

            _logger.LogInformation(
                "✅ Webhook başarıyla teslim edildi. EventId: {EventId}, Süre: {Duration} ms, HTTP Kodu: {StatusCode}",
                message.EventId,
                result.ResponseTimeMs,
                result.HttpStatusCode);
        }
        else
        {
            // Eğer 5. ve son deneme de başarısız olduysa Dead Letter olarak işaretle
            if (message.AttemptNumber >= 5)
            {
                attempt.MarkAsDeadLettered(result.HttpStatusCode, result.ResponseTimeMs, result.ErrorMessage);

                _logger.LogError(
                    "💀 Webhook 5 denemede de teslim edilemedi! Dead Letter Queue (DLQ)'ya aktarıldı. EventId: {EventId}, Hata: {Error}",
                    message.EventId,
                    result.ErrorMessage);
            }
            else
            {
                // Bir sonraki tahmini deneme zamanını hesapla
                var nextRetrySeconds = Math.Pow(2, message.AttemptNumber) * 3;
                var nextRetryAt = DateTime.UtcNow.AddSeconds(nextRetrySeconds);

                attempt.MarkAsFailed(result.HttpStatusCode, result.ResponseTimeMs, result.ErrorMessage, nextRetryAt);

                _logger.LogWarning(
                    "⚠️ Webhook {AttemptNumber}. denemede başarısız oldu. Bir sonraki deneme: {NextRetry} UTC. Hata: {Error}",
                    message.AttemptNumber,
                    nextRetryAt,
                    result.ErrorMessage);
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
