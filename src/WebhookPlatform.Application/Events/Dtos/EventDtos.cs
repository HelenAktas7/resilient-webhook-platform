using System.Text.Json;

namespace WebhookPlatform.Application.Events.Dtos;

public record PublishEventRequest(
    string IdempotencyKey,
    string EventType,
    JsonElement Data
);

public record PublishEventResponse(
    Guid EventId,
    string IdempotencyKey,
    string EventType,
    string Status,
    int MatchedSubscriptionsCount,
    DateTime CreatedAtUtc,
    string Message
);
