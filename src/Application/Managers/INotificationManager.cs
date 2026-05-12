namespace Notifications.Application.Managers;

/// <summary>Write-side operations for user notifications.</summary>
public interface INotificationManager
{
    Task MarkReadAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
