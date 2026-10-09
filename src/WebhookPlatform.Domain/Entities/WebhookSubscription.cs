namespace WebhookPlatform.Domain.Entities;

/// <summary>
/// Müşterinin belirli olayları dinlemek için kaydettiği Webhook hedef uç noktası.
/// </summary>
public class WebhookSubscription
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Aboneliği tanıtan isim (Örn: "Trendyol Sipariş Entegrasyonu").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Webhook isteklerinin gönderileceği hedef URL (Örn: "https://api.musteri.com/webhooks").
    /// </summary>
    public string TargetUrl { get; private set; } = string.Empty;

    /// <summary>
    /// İstek gövdesini HMAC SHA-256 ile imzalamak için kullanılan gizli anahtar.
    /// </summary>
    public string SecretKey { get; private set; } = string.Empty;

    /// <summary>
    /// Bu aboneliğin dinlediği olay tipleri (Örn: ["order.created", "payment.succeeded"]).
    /// Virgülle ayrılmış string veya JSON olarak saklanır.
    /// </summary>
    public List<string> SubscribedEventTypes { get; private set; } = new();

    /// <summary>
    /// Aboneliğin aktif olup olmadığı.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Bu hedefe dakikada atılabilecek maksimum istek sayısı (Rate Limiting).
    /// </summary>
    public int RateLimitPerMinute { get; private set; } = 60;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // EF Core için parametresiz constructor
    private WebhookSubscription() { }

    public WebhookSubscription(
        string name, 
        string targetUrl, 
        string secretKey, 
        List<string> subscribedEventTypes, 
        int rateLimitPerMinute = 60)
    {
        Id = Guid.NewGuid();
        Name = name;
        TargetUrl = targetUrl;
        SecretKey = secretKey;
        SubscribedEventTypes = subscribedEventTypes ?? new List<string>();
        RateLimitPerMinute = rateLimitPerMinute > 0 ? rateLimitPerMinute : 60;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string targetUrl, List<string> subscribedEventTypes, int rateLimitPerMinute, bool isActive)
    {
        Name = name;
        TargetUrl = targetUrl;
        SubscribedEventTypes = subscribedEventTypes;
        RateLimitPerMinute = rateLimitPerMinute;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
