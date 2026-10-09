using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Application.Subscriptions.Dtos;
using WebhookPlatform.Domain.Entities;

namespace WebhookPlatform.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/subscriptions")
            .WithTags("Webhook Subscriptions");

        // 1. Yeni Webhook Aboneliği Oluştur
        group.MapPost("/", async (CreateSubscriptionRequest request, IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TargetUrl))
            {
                return Results.BadRequest(new { error = "Name ve TargetUrl alanları zorunludur." });
            }

            if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return Results.BadRequest(new { error = "Geçerli bir HTTP veya HTTPS TargetUrl giriniz." });
            }

            // Secret key verilmediyse otomatik güvenli 32-byte hex anahtar üret (whsec_...)
            var secretKey = string.IsNullOrWhiteSpace(request.CustomSecretKey)
                ? $"whsec_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}"
                : request.CustomSecretKey;

            var subscription = new WebhookSubscription(
                request.Name,
                request.TargetUrl,
                secretKey,
                request.SubscribedEventTypes ?? new List<string>(),
                request.RateLimitPerMinute
            );

            dbContext.Subscriptions.Add(subscription);
            await dbContext.SaveChangesAsync(ct);

            var response = new SubscriptionResponse(
                subscription.Id,
                subscription.Name,
                subscription.TargetUrl,
                subscription.SecretKey,
                subscription.SubscribedEventTypes,
                subscription.IsActive,
                subscription.RateLimitPerMinute,
                subscription.CreatedAtUtc,
                subscription.UpdatedAtUtc
            );

            return Results.Created($"/api/v1/subscriptions/{subscription.Id}", response);
        })
        .WithName("CreateSubscription")
        .WithSummary("Yeni bir webhook abonesi kaydeder ve HMAC gizli anahtarı üretir.");

        // 2. Tüm Abonelikleri Listele
        group.MapGet("/", async (IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var subscriptions = await dbContext.Subscriptions
                .OrderByDescending(s => s.CreatedAtUtc)
                .Select(s => new SubscriptionResponse(
                    s.Id,
                    s.Name,
                    s.TargetUrl,
                    s.SecretKey,
                    s.SubscribedEventTypes,
                    s.IsActive,
                    s.RateLimitPerMinute,
                    s.CreatedAtUtc,
                    s.UpdatedAtUtc
                ))
                .ToListAsync(ct);

            return Results.Ok(subscriptions);
        })
        .WithName("GetSubscriptions")
        .WithSummary("Tüm webhook abonelerini listeler.");

        // 3. ID ile Tek Bir Aboneliği Getir
        group.MapGet("/{id:guid}", async (Guid id, IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var subscription = await dbContext.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (subscription is null)
                return Results.NotFound(new { error = $"Abonelik bulunamadı: {id}" });

            var response = new SubscriptionResponse(
                subscription.Id,
                subscription.Name,
                subscription.TargetUrl,
                subscription.SecretKey,
                subscription.SubscribedEventTypes,
                subscription.IsActive,
                subscription.RateLimitPerMinute,
                subscription.CreatedAtUtc,
                subscription.UpdatedAtUtc
            );

            return Results.Ok(response);
        })
        .WithName("GetSubscriptionById")
        .WithSummary("Belirli bir webhook abonesinin detaylarını getirir.");

        // 4. Aboneliği Sil (veya Deaktif Et)
        group.MapDelete("/{id:guid}", async (Guid id, IWebhookDbContext dbContext, CancellationToken ct) =>
        {
            var subscription = await dbContext.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (subscription is null)
                return Results.NotFound(new { error = $"Abonelik bulunamadı: {id}" });

            dbContext.Subscriptions.Remove(subscription);
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeleteSubscription")
        .WithSummary("Webhook abonesini siler.");
    }
}
