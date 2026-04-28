using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsHouseholdOwnershipTransferredConsumer : IConsumer<BillsHouseholdOwnershipTransferredEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsHouseholdOwnershipTransferredConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<BillsHouseholdOwnershipTransferredEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.NewOwnerId,
            EventType: "bills.household.ownership_transferred",
            Title: "You are now the household owner",
            Message: "Household ownership has been transferred to you",
            DeepLink: $"/bills/households/{msg.HouseholdId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdOwnershipTransferredEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
