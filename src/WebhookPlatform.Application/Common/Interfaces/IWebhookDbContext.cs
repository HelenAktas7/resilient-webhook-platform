using Microsoft.EntityFrameworkCore;
using WebhookPlatform.Domain.Entities;

namespace WebhookPlatform.Application.Common.Interfaces;

/// <summary>
/// Veritabanı işlemlerini soyutlayan arayüz (Clean Architecture Dependency Inversion).
/// </summary>
public interface IWebhookDbContext
{
    DbSet<WebhookSubscription> Subscriptions { get; }
    DbSet<WebhookEvent> Events { get; }
    DbSet<WebhookDeliveryAttempt> DeliveryAttempts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
