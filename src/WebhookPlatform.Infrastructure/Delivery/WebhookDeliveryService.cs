using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using WebhookPlatform.Application.Common.Interfaces;
using WebhookPlatform.Application.Common.Messages;

namespace WebhookPlatform.Infrastructure.Delivery;

/// <summary>
/// Polly v8 Resilience Pipeline ile donatılmış HTTP Webhook İletim Servisi.
/// Exponential Backoff, Jitter, Timeout ve HMAC imza yönetimini gerçekleştirir.
/// </summary>
public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly HttpClient _httpClient;
    private readonly IWebhookSigner _signer;
    private readonly ILogger<WebhookDeliveryService> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    public WebhookDeliveryService(
        HttpClient httpClient,
        IWebhookSigner signer,
        ILogger<WebhookDeliveryService> logger)
    {
        _httpClient = httpClient;
        _signer = signer;
        _logger = logger;

        // Polly v8 Dayanıklılık Hattı (Resilience Pipeline) Yapılandırması
        _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // 1. Kural: 5 saniye içinde yanıt gelmezse zaman aşımına uğrat (Timeout)
            .AddTimeout(TimeSpan.FromSeconds(5))
            // 2. Kural: 500+ Hata veya Ağ Hatası aldığında Exponential Backoff + Jitter ile tekrar dene
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "⚠️ Webhook teslimatı başarısız oldu. Polly {AttemptNumber}. yeniden denemeyi başlatıyor. Bekleme süresi: {Delay} ms. Hata: {Error}",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<DeliveryExecutionResult> DeliverAsync(WebhookDispatchMessage message, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signer.GenerateSignature(message.PayloadJson, message.SecretKey, timestamp);

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        string? responseBody = null;

        try
        {
            // Polly Resilience Pipeline üzerinden HTTP çağrısını yürüt
            response = await _resiliencePipeline.ExecuteAsync(async state =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, message.TargetUrl);
                
                request.Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json");

                // Standart Güvenlik Header'ları
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Timestamp", timestamp.ToString());
                request.Headers.Add("X-Webhook-Id", message.EventId.ToString());
                request.Headers.Add("X-Webhook-Event", message.EventType);
                request.Headers.Add("User-Agent", "ResilientWebhookEngine/1.0");

                return await _httpClient.SendAsync(request, state);
            }, cancellationToken);

            stopwatch.Stop();

            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            var isSuccess = response.IsSuccessStatusCode;
            return new DeliveryExecutionResult(
                IsSuccess: isSuccess,
                HttpStatusCode: (int)response.StatusCode,
                ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                ResponseBody: responseBody,
                ErrorMessage: isSuccess ? null : $"Hedef sunucu başarısız HTTP kodu döndü: {(int)response.StatusCode}"
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ Webhook iletimi tamamen başarısız oldu. Hedef URL: {TargetUrl}", message.TargetUrl);

            return new DeliveryExecutionResult(
                IsSuccess: false,
                HttpStatusCode: response is not null ? (int)response.StatusCode : null,
                ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                ResponseBody: responseBody,
                ErrorMessage: ex.Message
            );
        }
    }
}
