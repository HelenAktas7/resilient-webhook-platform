using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Infrastructure.Delivery;
using WebhookPlatform.Infrastructure.Persistence;
using WebhookPlatform.Infrastructure.RateLimiting;
using WebhookPlatform.Infrastructure.Security;

namespace WebhookPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' bulunamadı.");

        services.AddDbContext<WebhookDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(WebhookDbContext).Assembly.FullName)));

        services.AddScoped<IWebhookDbContext>(provider => provider.GetRequiredService<WebhookDbContext>());

        // HMAC Güvenlik & İmza Servisi (Singleton)
        services.AddSingleton<IWebhookSigner, HmacWebhookSigner>();

        // Polly Dayanıklılık Motoru ile donatılmış HTTP İletim Servisi
        services.AddHttpClient<IWebhookDeliveryService, WebhookDeliveryService>();

        // Redis Bağlantısı ve Rate Limiter (Singleton)
        var redisConnection = configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        return services;
    }
}
