using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Application.Common.Messages;
using WebhookPlatform.Domain.Enums;

namespace WebhookPlatform.Api.Endpoints;

public static class DlqEndpoints
{
    public static void MapDlqEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dlq")
            .WithTags("Dead Letter Queue (DLQ) & Replay");

        // 1. DLQ'daki (Başarısız) Kayıtları Listele
        group.MapGet("/", async (IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var deadLetters = await dbContext.DeliveryAttempts
                .Include(a => a.WebhookEvent)
                .Include(a => a.WebhookSubscription)
                .Where(a => a.Status == DeliveryStatus.DeadLettered || a.Status == DeliveryStatus.Failed)
                .OrderByDescending(a => a.AttemptedAtUtc)
                .Take(100)
                .Select(a => new
                {
                    AttemptId = a.Id,
                    EventId = a.WebhookEventId,
                    SubscriptionId = a.WebhookSubscriptionId,
                    SubscriptionName = a.WebhookSubscription != null ? a.WebhookSubscription.Name : "Bilinmeyen",
                    TargetUrl = a.WebhookSubscription != null ? a.WebhookSubscription.TargetUrl : "",
                    EventType = a.WebhookEvent != null ? a.WebhookEvent.EventType : "",
                    PayloadJson = a.WebhookEvent != null ? a.WebhookEvent.PayloadJson : "",
                    a.AttemptNumber,
                    Status = a.Status.ToString(),
                    a.HttpStatusCode,
                    a.ResponseTimeMs,
                    a.ErrorMessage,
                    a.AttemptedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(deadLetters);
        })
        .WithName("GetDeadLetters")
        .WithSummary("Teslim edilemeyen (DLQ) tüm webhook kayıtlarını listeler.");

        // 2. Tek Bir Başarısız Mesajı Yeniden Tetikle (Replay)
        group.MapPost("/{id:guid}/replay", async (
            Guid id, 
            IWebhookDbContext dbContext, 
            IPublishEndpoint publishEndpoint, 
            CancellationToken ct) =>
        {
            var attempt = await dbContext.DeliveryAttempts
                .Include(a => a.WebhookEvent)
                .Include(a => a.WebhookSubscription)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (attempt is null)
                return Results.NotFound(new { error = $"Teslimat denemesi bulunamadı: {id}" });

            if (attempt.WebhookEvent is null || attempt.WebhookSubscription is null)
                return Results.BadRequest(new { error = "İlişkili olay veya abonelik kaydı eksik." });

            // RabbitMQ kuyruğuna 1. deneme olarak yeniden fırlat!
            await publishEndpoint.Publish(new WebhookDispatchMessage
            {
                EventId = attempt.WebhookEvent.Id,
                SubscriptionId = attempt.WebhookSubscription.Id,
                TargetUrl = attempt.WebhookSubscription.TargetUrl,
                SecretKey = attempt.WebhookSubscription.SecretKey,
                EventType = attempt.WebhookEvent.EventType,
                PayloadJson = attempt.WebhookEvent.PayloadJson,
                AttemptNumber = 1,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);

            return Results.Ok(new
            {
                Message = "Mesaj başarıyla yeniden kuyruğa (Replay) aktarıldı.",
                attempt.WebhookEvent.Id,
                attempt.WebhookSubscription.TargetUrl
            });
        })
        .WithName("ReplayDeadLetter")
        .WithSummary("Başarısız bir webhook mesajını baştan sıfırdan kuyruğa sokar.");

        // 3. Toplu Kurtarma (Replay All)
        group.MapPost("/replay-all", async (
            IWebhookDbContext dbContext, 
            IPublishEndpoint publishEndpoint, 
            CancellationToken ct) =>
        {
            var deadLetters = await dbContext.DeliveryAttempts
                .Include(a => a.WebhookEvent)
                .Include(a => a.WebhookSubscription)
                .Where(a => a.Status == DeliveryStatus.DeadLettered)
                .ToListAsync(ct);

            int replayedCount = 0;
            foreach (var item in deadLetters)
            {
                if (item.WebhookEvent != null && item.WebhookSubscription != null)
                {
                    await publishEndpoint.Publish(new WebhookDispatchMessage
                    {
                        EventId = item.WebhookEvent.Id,
                        SubscriptionId = item.WebhookSubscription.Id,
                        TargetUrl = item.WebhookSubscription.TargetUrl,
                        SecretKey = item.WebhookSubscription.SecretKey,
                        EventType = item.WebhookEvent.EventType,
                        PayloadJson = item.WebhookEvent.PayloadJson,
                        AttemptNumber = 1,
                        CreatedAtUtc = DateTime.UtcNow
                    }, ct);
                    replayedCount++;
                }
            }

            return Results.Ok(new
            {
                Message = $"{replayedCount} adet DLQ mesajı başarıyla yeniden kuyruğa fırlatıldı.",
                Count = replayedCount
            });
        })
        .WithName("ReplayAllDeadLetters")
        .WithSummary("DLQ'daki tüm ölü mesajları topluca yeniden kuyruğa sokar.");

        // 4. Platform İstatistikleri & Dashboard Metrikleri
        group.MapGet("/stats", async (IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var totalEvents = await dbContext.Events.CountAsync(ct);
            var totalAttempts = await dbContext.DeliveryAttempts.CountAsync(ct);
            var successfulAttempts = await dbContext.DeliveryAttempts.CountAsync(a => a.Status == DeliveryStatus.Success, ct);
            var failedAttempts = await dbContext.DeliveryAttempts.CountAsync(a => a.Status == DeliveryStatus.Failed, ct);
            var deadLetters = await dbContext.DeliveryAttempts.CountAsync(a => a.Status == DeliveryStatus.DeadLettered, ct);

            var avgResponseTime = totalAttempts > 0 
                ? await dbContext.DeliveryAttempts.Where(a => a.ResponseTimeMs > 0).AverageAsync(a => (double?)a.ResponseTimeMs, ct) ?? 0 
                : 0;

            var successRate = totalAttempts > 0 
                ? Math.Round((double)successfulAttempts / totalAttempts * 100, 2) 
                : 100.0;

            return Results.Ok(new
            {
                TotalEvents = totalEvents,
                TotalAttempts = totalAttempts,
                SuccessfulDeliveries = successfulAttempts,
                RetryingDeliveries = failedAttempts,
                DeadLetters = deadLetters,
                SuccessRatePercentage = $"{successRate}%",
                AverageResponseTimeMs = Math.Round(avgResponseTime, 2)
            });
        })
        .WithName("GetPlatformStats")
        .WithSummary("Sistemin genel başarı oranı, ortalama yanıt süresi ve DLQ metriklerini getirir.");
    }
}
