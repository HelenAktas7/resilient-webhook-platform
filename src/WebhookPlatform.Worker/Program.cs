using MassTransit;
using WebhookPlatform.Infrastructure;
using WebhookPlatform.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Clean Architecture - Infrastructure bağımlılıkları (Postgres DbContext, Polly DeliveryService, HMAC)
builder.Services.AddInfrastructureServices(builder.Configuration);

// MassTransit RabbitMQ Dinleyicisi
builder.Services.AddMassTransit(x =>
{
    // Kuyruk dinleyicisi Consumer'ı kaydet
    x.AddConsumer<WebhookDispatchConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5672, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // "webhook-dispatch-queue" adında otomatik kuyruk aç ve consumer'ı bağla
        cfg.ReceiveEndpoint("webhook-dispatch-queue", e =>
        {
            // Eşzamanlı maksimum 16 webhook mesajını işle
            e.ConcurrentMessageLimit = 16;
            e.ConfigureConsumer<WebhookDispatchConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
