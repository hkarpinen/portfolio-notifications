using Notifications.Application.Contracts;
using Notifications.Application.Services;

namespace Infrastructure.Notifications;

internal sealed class NotificationPublisher : INotificationPublisher
{
    private readonly INotificationRepository _repository;
    private readonly INotificationDispatcher _dispatcher;

    public NotificationPublisher(INotificationRepository repository, INotificationDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    public async Task PublishAsync(NotificationStreamEventDto notification, CancellationToken cancellationToken = default)
    {
        await _repository.PersistAsync(notification, cancellationToken);
        _dispatcher.Dispatch(notification.RecipientUserId, notification);
    }
}
