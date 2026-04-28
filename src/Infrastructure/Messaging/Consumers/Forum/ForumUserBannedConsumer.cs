using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class ForumUserBannedConsumer : IConsumer<ForumUserBannedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumUserBannedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumUserBannedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var message = msg.Reason is not null
            ? $"You have been banned: {msg.Reason}"
            : "You have been banned from this community";

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.user.banned",
            Title: "Community ban",
            Message: message,
            DeepLink: null,
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumUserBannedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
