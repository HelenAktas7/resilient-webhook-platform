namespace WebhookPlatform.Application.Subscriptions.Dtos;

public record CreateSubscriptionRequest(
    string Name,
    string TargetUrl,
    List<string> SubscribedEventTypes,
    int RateLimitPerMinute = 60,
    string? CustomSecretKey = null
);

public record SubscriptionResponse(
    Guid Id,
    string Name,
    string TargetUrl,
    string SecretKey,
    List<string> SubscribedEventTypes,
    bool IsActive,
    int RateLimitPerMinute,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
