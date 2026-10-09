using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Domain.Entities;

namespace WebhookPlatform.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL veritabanı bağlantısı ve Entity yapılandırmalarını barındıran EF Core DbContext.
/// </summary>
public class WebhookDbContext : DbContext, IWebhookDbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options)
    {
    }

    public DbSet<WebhookSubscription> Subscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookEvent> Events => Set<WebhookEvent>();
    public DbSet<WebhookDeliveryAttempt> DeliveryAttempts => Set<WebhookDeliveryAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. WebhookSubscription Yapılandırması
        modelBuilder.Entity<WebhookSubscription>(builder =>
        {
            builder.ToTable("webhook_subscriptions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(s => s.TargetUrl)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(s => s.SecretKey)
                .IsRequired()
                .HasMaxLength(256);

            // SubscribedEventTypes listesini JSON string olarak PostgreSQL'de saklayalım
            builder.Property(s => s.SubscribedEventTypes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("text");

            builder.HasIndex(s => s.IsActive);
        });

        // 2. WebhookEvent Yapılandırması
        modelBuilder.Entity<WebhookEvent>(builder =>
        {
            builder.ToTable("webhook_events");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.PayloadJson)
                .IsRequired()
                .HasColumnType("text");

            // Idempotency kontrolü için Unique Index (Mükerrer istekleri DB seviyesinde engeller)
            builder.HasIndex(e => e.IdempotencyKey)
                .IsUnique();

            builder.HasIndex(e => e.EventType);
            builder.HasIndex(e => e.CreatedAtUtc);
        });

        // 3. WebhookDeliveryAttempt Yapılandırması
        modelBuilder.Entity<WebhookDeliveryAttempt>(builder =>
        {
            builder.ToTable("webhook_delivery_attempts");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.ResponseBody)
                .HasMaxLength(2000);

            builder.Property(a => a.ErrorMessage)
                .HasMaxLength(2000);

            builder.HasOne(a => a.WebhookEvent)
                .WithMany()
                .HasForeignKey(a => a.WebhookEventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.WebhookSubscription)
                .WithMany()
                .HasForeignKey(a => a.WebhookSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Kuyruk ve arama performansı için kritik indeksler
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => a.NextRetryAtUtc);
            builder.HasIndex(a => a.AttemptedAtUtc);
        });
    }
}
