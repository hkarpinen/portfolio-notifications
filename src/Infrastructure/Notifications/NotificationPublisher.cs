using Microsoft.Extensions.Logging;
using Notifications.Application.Commands;
using Notifications.Application.Contracts;
using Notifications.Application.Repositories;
using Notifications.Application.Services;

namespace Infrastructure.Notifications;

internal sealed class NotificationPublisher : INotificationPublisher
{
    private readonly INotificationRepository _repository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        INotificationRepository repository,
        INotificationDispatcher dispatcher,
        ILogger<NotificationPublisher> logger)
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task PublishAsync(PublishNotificationCommand command, CancellationToken cancellationToken = default)
    {
        var notification = new NotificationStreamEventDto(
            command.EventId,
            command.RecipientUserId,
            command.EventType,
            command.Title,
            command.Message,
            command.DeepLink,
            command.OccurredAt,
            IsRead: false);

        await _repository.PersistAsync(notification, cancellationToken);

        try
        {
            _dispatcher.Dispatch(notification.RecipientUserId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "In-memory dispatch failed for {EventId}", notification.EventId);
        }
    }
}
