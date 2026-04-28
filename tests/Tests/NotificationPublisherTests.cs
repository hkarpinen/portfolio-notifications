using Infrastructure.Notifications;
using NSubstitute;
using Notifications.Application.Contracts;
using Notifications.Application.Services;

namespace Tests;

public class NotificationPublisherTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly NotificationPublisher _publisher;

    public NotificationPublisherTests()
    {
        _publisher = new NotificationPublisher(_repository, _dispatcher);
    }

    private static NotificationStreamEventDto MakeNotification(Guid? recipientId = null) =>
        new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: recipientId ?? Guid.NewGuid(),
            EventType: "test.event",
            Title: "Test",
            Message: "A test notification",
            DeepLink: "/test",
            OccurredAt: DateTime.UtcNow,
            IsRead: false);

    [Fact]
    public async Task PublishAsync_ShouldPersistNotification()
    {
        // Arrange
        var notification = MakeNotification();

        // Act
        await _publisher.PublishAsync(notification);

        // Assert
        await _repository.Received(1).PersistAsync(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldDispatchNotification()
    {
        // Arrange
        var notification = MakeNotification();

        // Act
        await _publisher.PublishAsync(notification);

        // Assert
        _dispatcher.Received(1).Dispatch(notification.RecipientUserId, notification);
    }

    [Fact]
    public async Task PublishAsync_ShouldPersistBeforeDispatching()
    {
        // Arrange
        var notification = MakeNotification();
        var order = new List<string>();

        _repository.PersistAsync(Arg.Any<NotificationStreamEventDto>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                order.Add("persist");
                return Task.CompletedTask;
            });

        _dispatcher.When(d => d.Dispatch(Arg.Any<Guid>(), Arg.Any<NotificationStreamEventDto>()))
            .Do(_ => order.Add("dispatch"));

        // Act
        await _publisher.PublishAsync(notification);

        // Assert
        Assert.Equal(new[] { "persist", "dispatch" }, order);
    }

    [Fact]
    public async Task PublishAsync_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var notification = MakeNotification();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _publisher.PublishAsync(notification, token);

        // Assert
        await _repository.Received(1).PersistAsync(notification, token);
    }
}
