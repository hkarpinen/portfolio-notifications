using Infrastructure.Notifications;
using Notifications.Application.Contracts;

namespace Tests;

public class InMemoryNotificationDispatcherTests
{
    private readonly InMemoryNotificationDispatcher _dispatcher = new();

    [Fact]
    public void Dispatch_WithNoSubscribers_ShouldNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = MakeNotification(userId);

        // Act / Assert (should not throw)
        _dispatcher.Dispatch(userId, notification);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldReceiveDispatchedNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = MakeNotification(userId);
        using var cts = new CancellationTokenSource();

        // Act - subscribe and dispatch
        var received = new List<NotificationStreamEventDto>();

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var item in _dispatcher.SubscribeAsync(userId, cts.Token))
            {
                received.Add(item);
                cts.Cancel(); // stop after first item
            }
        });

        // Give the subscription a moment to register
        await Task.Delay(50);
        _dispatcher.Dispatch(userId, notification);

        try { await subscribeTask; } catch (OperationCanceledException) { }

        // Assert
        Assert.Single(received);
        Assert.Equal(notification.EventId, received[0].EventId);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldNotReceiveNotificationsForOtherUsers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var received = new List<NotificationStreamEventDto>();

        var subscribeTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in _dispatcher.SubscribeAsync(userId, cts.Token))
                    received.Add(item);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(50);

        // Dispatch to a different user
        _dispatcher.Dispatch(otherUserId, MakeNotification(otherUserId));

        await subscribeTask;

        // Assert - should receive nothing
        Assert.Empty(received);
    }

    [Fact]
    public async Task Dispatch_WithMultipleSubscribers_ShouldDeliverToAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = MakeNotification(userId);

        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        var received1 = new List<NotificationStreamEventDto>();
        var received2 = new List<NotificationStreamEventDto>();

        // Start two subscribers
        var task1 = Task.Run(async () =>
        {
            await foreach (var item in _dispatcher.SubscribeAsync(userId, cts1.Token))
            {
                received1.Add(item);
                cts1.Cancel();
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var item in _dispatcher.SubscribeAsync(userId, cts2.Token))
            {
                received2.Add(item);
                cts2.Cancel();
            }
        });

        await Task.Delay(50);
        _dispatcher.Dispatch(userId, notification);

        try { await task1; } catch (OperationCanceledException) { }
        try { await task2; } catch (OperationCanceledException) { }

        // Assert
        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Equal(notification.EventId, received1[0].EventId);
        Assert.Equal(notification.EventId, received2[0].EventId);
    }

    [Fact]
    public async Task SubscribeAsync_WhenCancelled_ShouldCleanUpSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var subscribeTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in _dispatcher.SubscribeAsync(userId, cts.Token)) { }
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(50);

        // Act - cancel the subscription
        cts.Cancel();
        await subscribeTask;

        // Assert - dispatching after unsubscribe should not throw
        _dispatcher.Dispatch(userId, MakeNotification(userId));
    }

    private static NotificationStreamEventDto MakeNotification(Guid recipientId) =>
        new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: recipientId,
            EventType: "test.event",
            Title: "Test",
            Message: "A test notification",
            DeepLink: null,
            OccurredAt: DateTime.UtcNow,
            IsRead: false);
}
