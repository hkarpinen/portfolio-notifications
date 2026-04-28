using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsHouseholdMemberRemovedConsumer : IConsumer<BillsHouseholdMemberRemovedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsHouseholdMemberRemovedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<BillsHouseholdMemberRemovedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var existing = await _db.HouseholdMembers
            .FirstOrDefaultAsync(x => x.HouseholdId == msg.HouseholdId && x.UserId == msg.UserId, context.CancellationToken);
        if (existing is not null) existing.IsActive = false;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "bills.member.removed",
            Title: "Removed from household",
            Message: "You have been removed from a household",
            DeepLink: "/bills",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdMemberRemovedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
