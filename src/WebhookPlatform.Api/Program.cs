using WebhookPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Clean Architecture - Infrastructure bağımlılıklarını ekle (Postgres, DbContext)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    Platform = "Resilient Webhook Platform",
    Status = "Healthy",
    Version = "v1.0"
}));

app.Run();
