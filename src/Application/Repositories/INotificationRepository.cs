using Notifications.Application.Contracts;

namespace Notifications.Application.Repositories;

/// <summary>Persists and retrieves notifications.</summary>
public interface INotificationRepository
{
    Task PersistAsync(NotificationStreamEventDto dto, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
