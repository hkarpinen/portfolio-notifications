using Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Notifications.Application.Commands;
using Notifications.Application.Contracts;
using Notifications.Application.Repositories;
using Notifications.Application.Services;

namespace Tests;

public class NotificationPublisherTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly NotificationPublisher _publisher;

    public NotificationPublisherTests()
    {
        _publisher = new NotificationPublisher(_repository, _dispatcher, NullLogger<NotificationPublisher>.Instance);
    }

    private static PublishNotificationCommand MakeCommand(Guid? recipientId = null) =>
        new PublishNotificationCommand(
            EventId: Guid.NewGuid(),
            RecipientUserId: recipientId ?? Guid.NewGuid(),
            EventType: "test.event",
            Title: "Test",
            Message: "A test notification",
            DeepLink: "/test",
            OccurredAt: DateTime.UtcNow);

    [Fact]
    public async Task PublishAsync_ShouldPersistNotification()
    {
        // Arrange
        var command = MakeCommand();

        // Act
        await _publisher.PublishAsync(command);

        // Assert
        await _repository.Received(1).PersistAsync(
            Arg.Is<NotificationStreamEventDto>(n =>
                n.EventId == command.EventId &&
                n.RecipientUserId == command.RecipientUserId &&
                n.EventType == command.EventType),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldDispatchNotification()
    {
        // Arrange
        var command = MakeCommand();

        // Act
        await _publisher.PublishAsync(command);

        // Assert
        _dispatcher.Received(1).Dispatch(
            command.RecipientUserId,
            Arg.Is<NotificationStreamEventDto>(n => n.EventId == command.EventId));
    }

    [Fact]
    public async Task PublishAsync_ShouldPersistBeforeDispatching()
    {
        // Arrange
        var command = MakeCommand();
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
        await _publisher.PublishAsync(command);

        // Assert
        Assert.Equal(new[] { "persist", "dispatch" }, order);
    }

    [Fact]
    public async Task PublishAsync_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var command = MakeCommand();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _publisher.PublishAsync(command, token);

        // Assert
        await _repository.Received(1).PersistAsync(
            Arg.Is<NotificationStreamEventDto>(n => n.EventId == command.EventId),
            token);
    }
}
