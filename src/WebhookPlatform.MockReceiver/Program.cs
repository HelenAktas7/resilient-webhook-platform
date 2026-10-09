using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Hafızada son gelen webhook isteklerini ve deneme sayaçlarını tutalım
var receivedLogs = new ConcurrentBag<object>();
var flakyAttemptCounters = new ConcurrentDictionary<string, int>();

// 1. Sağlıklı Müşteri (Her zaman 200 OK)
app.MapPost("/api/mock/webhook/success", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var signature = context.Request.Headers["X-Webhook-Signature"].ToString();
    var eventId = context.Request.Headers["X-Webhook-Id"].ToString();

    var log = new
    {
        Target = "Success Receiver",
        EventId = eventId,
        Signature = signature,
        ReceivedAt = DateTime.UtcNow,
        Status = 200
    };
    receivedLogs.Add(log);

    return Results.Ok(new { message = "Webhook başarıyla alındı ve işlendi.", eventId });
});

// 2. Çökmüş Müşteri (Her zaman 500 Hata)
app.MapPost("/api/mock/webhook/fail", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var eventId = context.Request.Headers["X-Webhook-Id"].ToString();

    return Results.Problem(
        statusCode: 500,
        title: "Internal Server Error",
        detail: "Müşteri veritabanı çöktü! Webhook işlenemiyor.");
});

// 3. Yavaş Müşteri (Zaman Aşımı / Timeout Simülatörü - 6 saniye uyur)
app.MapPost("/api/mock/webhook/delay", async (HttpContext context) =>
{
    // Polly'nin 5 sn timeout kuralını tetiklemek için 6 saniye beklet
    await Task.Delay(6000);
    return Results.Ok(new { message = "Çok geç yanıt verildi." });
});

// 4. Kararsız Müşteri (İlk 2 denemede 500 döner, 3. denemede düzelir!)
app.MapPost("/api/mock/webhook/flaky", async (HttpContext context) =>
{
    var eventId = context.Request.Headers["X-Webhook-Id"].ToString();
    var key = string.IsNullOrWhiteSpace(eventId) ? "default" : eventId;

    var count = flakyAttemptCounters.AddOrUpdate(key, 1, (_, current) => current + 1);

    if (count < 3)
    {
        return Results.Problem(
            statusCode: 500,
            title: "Temporary Glitch",
            detail: $"Geçici hata! Deneme sayısı: {count}. Lütfen tekrar deneyiniz.");
    }

    // 3. denemede başarıya ulaştı!
    flakyAttemptCounters.TryRemove(key, out _);
    return Results.Ok(new { message = $"3. denemede başarıyla kurtarıldı! (Deneme: {count})", eventId });
});

// Gelen son istekleri listele
app.MapGet("/api/mock/webhook/received", () => Results.Ok(receivedLogs.Take(20)));

app.MapGet("/", () => Results.Ok(new
{
    Service = "Mock Webhook Receiver Simulator",
    Status = "Running on port 5050",
    Endpoints = new[]
    {
        "POST /api/mock/webhook/success (200 OK)",
        "POST /api/mock/webhook/fail (500 Error - Tests Polly & DLQ)",
        "POST /api/mock/webhook/delay (Timeout - Tests Polly Timeout)",
        "POST /api/mock/webhook/flaky (Fails 2 times, succeeds on 3rd!)"
    }
}));

app.Run("http://localhost:5050");
