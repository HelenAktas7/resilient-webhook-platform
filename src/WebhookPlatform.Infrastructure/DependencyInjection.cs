using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Infrastructure.Persistence;
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

        // HMAC Güvenlik & İmza Servisi (Thread-safe, Singleton)
        services.AddSingleton<IWebhookSigner, HmacWebhookSigner>();

        return services;
    }
}
