using Notifications.Application.Contracts;

namespace Notifications.Application.Services;

/// <summary>
/// Dispatches real-time notification events to connected SSE clients.
/// Implementations must be thread-safe and registered as Singleton.
/// </summary>
public interface INotificationDispatcher
{
    void Dispatch(Guid recipientUserId, NotificationStreamEventDto notification);
    IAsyncEnumerable<NotificationStreamEventDto> SubscribeAsync(Guid userId, CancellationToken cancellationToken);
}
