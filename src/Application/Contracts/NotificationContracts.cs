namespace Notifications.Application.Contracts;

/// <summary>Represents a single notification event streamed over SSE.</summary>
public sealed record NotificationStreamEventDto(
    Guid EventId,
    Guid RecipientUserId,
    string EventType,
    string Title,
    string Message,
    string? DeepLink,
    DateTime OccurredAt,
    bool IsRead);

/// <summary>Envelope returned by the list endpoint.</summary>
public sealed record NotificationStreamDto(
    IReadOnlyList<NotificationStreamEventDto> Items);
