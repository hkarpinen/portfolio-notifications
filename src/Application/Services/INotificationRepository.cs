using Notifications.Application.Contracts;

namespace Notifications.Application.Services;

/// <summary>Persists and retrieves notifications.</summary>
public interface INotificationRepository
{
    Task PersistAsync(NotificationStreamEventDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationStreamEventDto>> GetRecentAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
