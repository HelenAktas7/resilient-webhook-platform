using WebhookPlatform.Api.Endpoints;
using WebhookPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Clean Architecture - Infrastructure bağımlılıkları (Postgres, HMAC Signer vb.)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Root Health Endpoint
app.MapGet("/", () => Results.Ok(new
{
    Platform = "Resilient Webhook Platform",
    Status = "Healthy",
    Version = "v1.0",
    Timestamp = DateTime.UtcNow
}));

// API Endpoint Gruplarını Haritala
app.MapSubscriptionEndpoints();
app.MapEventEndpoints();

app.Run();
