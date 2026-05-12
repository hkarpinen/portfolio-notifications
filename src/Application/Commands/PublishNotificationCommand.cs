namespace Notifications.Application.Commands;

public sealed record PublishNotificationCommand(
    Guid EventId,
    Guid RecipientUserId,
    string EventType,
    string Title,
    string Message,
    string? DeepLink,
    DateTime OccurredAt);
