using Notifications.Application.Commands;

namespace Notifications.Application.Services;

/// <summary>Persists notifications and then fans them out to live subscribers.</summary>
public interface INotificationPublisher
{
    Task PublishAsync(PublishNotificationCommand command, CancellationToken cancellationToken = default);
}
