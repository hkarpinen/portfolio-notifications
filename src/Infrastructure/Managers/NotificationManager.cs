using Notifications.Application.Managers;
using Notifications.Application.Repositories;

namespace Infrastructure.Managers;

internal sealed class NotificationManager : INotificationManager
{
    private readonly INotificationRepository _repository;

    public NotificationManager(INotificationRepository repository)
    {
        _repository = repository;
    }

    public Task MarkReadAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
        => _repository.MarkReadAsync(eventId, userId, cancellationToken);

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
        => _repository.MarkAllReadAsync(userId, cancellationToken);
}
