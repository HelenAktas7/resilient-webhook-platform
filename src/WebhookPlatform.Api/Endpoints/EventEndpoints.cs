using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Application.Common.Messages;
using WebhookPlatform.Application.Events.Dtos;
using WebhookPlatform.Domain.Entities;

namespace WebhookPlatform.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/events")
            .WithTags("Webhook Ingestion & Events");

        // 1. Olay Giriş Kapısı (Publish Event with Idempotency & RabbitMQ Dispatch)
        group.MapPost("/publish", async (
            PublishEventRequest request, 
            IWebhookDbContext dbContext, 
            IPublishEndpoint publishEndpoint, 
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.EventType))
            {
                return Results.BadRequest(new { error = "IdempotencyKey ve EventType alanları zorunludur." });
            }

            var payloadString = request.Data.GetRawText();
            if (string.IsNullOrWhiteSpace(payloadString))
            {
                return Results.BadRequest(new { error = "Data (payload) alanı boş olamaz." });
            }

            // 1. Idempotency Kontrolü: Aynı anahtarla daha önce geldi mi?
            var existingEvent = await dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdempotencyKey == request.IdempotencyKey, ct);

            if (existingEvent is not null)
            {
                return Results.Ok(new PublishEventResponse(
                    existingEvent.Id,
                    existingEvent.IdempotencyKey,
                    existingEvent.EventType,
                    "DuplicateIgnored",
                    0,
                    existingEvent.CreatedAtUtc,
                    "Bu IdempotencyKey ile daha önce bir olay kaydedilmişti. Mükerrer işlem engellendi."
                ));
            }

            // 2. Yeni Olayı Veritabanına Kaydet
            var webhookEvent = new WebhookEvent(
                request.IdempotencyKey,
                request.EventType,
                payloadString
            );

            dbContext.Events.Add(webhookEvent);

            // 3. Bu EventType'ı dinleyen aktif aboneleri bul
            var activeSubscriptions = await dbContext.Subscriptions
                .Where(s => s.IsActive)
                .ToListAsync(ct);

            var matchedSubscriptions = activeSubscriptions
                .Where(s => s.SubscribedEventTypes.Count == 0 || 
                            s.SubscribedEventTypes.Contains("*") || 
                            s.SubscribedEventTypes.Contains(request.EventType))
                .ToList();

            await dbContext.SaveChangesAsync(ct);

            // 4. Eşleşen her bir abone için RabbitMQ kuyruğuna asenkron dağıtım mesajı fırlat!
            foreach (var sub in matchedSubscriptions)
            {
                await publishEndpoint.Publish(new WebhookDispatchMessage
                {
                    EventId = webhookEvent.Id,
                    SubscriptionId = sub.Id,
                    TargetUrl = sub.TargetUrl,
                    SecretKey = sub.SecretKey,
                    EventType = webhookEvent.EventType,
                    PayloadJson = webhookEvent.PayloadJson,
                    AttemptNumber = 1,
                    CreatedAtUtc = DateTime.UtcNow
                }, ct);
            }

            // 202 Accepted dön (İstek alındı ve arka plan kuyruğuna iletildi garantisi)
            var response = new PublishEventResponse(
                webhookEvent.Id,
                webhookEvent.IdempotencyKey,
                webhookEvent.EventType,
                "Accepted",
                matchedSubscriptions.Count,
                webhookEvent.CreatedAtUtc,
                $"Olay kabul edildi ve {matchedSubscriptions.Count} abone için RabbitMQ dağıtım sırasına alındı."
            );

            return Results.Accepted($"/api/v1/events/{webhookEvent.Id}", response);
        })
        .WithName("PublishEvent")
        .WithSummary("Sisteme yeni bir olay (Event) fırlatır, Idempotency uygular ve RabbitMQ kuyruğuna dağıtır.");

        // 2. Geçmiş Olayları Listele
        group.MapGet("/", async (IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var events = await dbContext.Events
                .OrderByDescending(e => e.CreatedAtUtc)
                .Take(50)
                .Select(e => new
                {
                    e.Id,
                    e.IdempotencyKey,
                    e.EventType,
                    e.PayloadJson,
                    e.CreatedAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(events);
        })
        .WithName("GetEvents")
        .WithSummary("Sisteme gelmiş son 50 olayı listeler.");

        // 3. Olay Detayı ve Teslimat Denemeleri
        group.MapGet("/{id:guid}", async (Guid id, IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var webhookEvent = await dbContext.Events
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (webhookEvent is null)
                return Results.NotFound(new { error = $"Olay bulunamadı: {id}" });

            var deliveryAttempts = await dbContext.DeliveryAttempts
                .Include(a => a.WebhookSubscription)
                .Where(a => a.WebhookEventId == id)
                .OrderBy(a => a.AttemptedAtUtc)
                .Select(a => new
                {
                    a.Id,
                    SubscriptionName = a.WebhookSubscription != null ? a.WebhookSubscription.Name : "Bilinmeyen",
                    TargetUrl = a.WebhookSubscription != null ? a.WebhookSubscription.TargetUrl : "",
                    a.AttemptNumber,
                    Status = a.Status.ToString(),
                    a.HttpStatusCode,
                    a.ResponseTimeMs,
                    a.ResponseBody,
                    a.ErrorMessage,
                    a.AttemptedAtUtc,
                    a.NextRetryAtUtc
                })
                .ToListAsync(ct);

            return Results.Ok(new
            {
                webhookEvent.Id,
                webhookEvent.IdempotencyKey,
                webhookEvent.EventType,
                webhookEvent.PayloadJson,
                webhookEvent.CreatedAtUtc,
                DeliveryAttempts = deliveryAttempts
            });
        })
        .WithName("GetEventById")
        .WithSummary("Belirli bir olayın detayını ve teslimat deneme geçmişini getirir.");
    }
}
