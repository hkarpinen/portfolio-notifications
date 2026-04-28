using Notifications.Application.Contracts;

namespace Notifications.Application.Services;

/// <summary>Persists notifications and then fans them out to live subscribers.</summary>
public interface INotificationPublisher
{
    Task PublishAsync(NotificationStreamEventDto notification, CancellationToken cancellationToken = default);
}
