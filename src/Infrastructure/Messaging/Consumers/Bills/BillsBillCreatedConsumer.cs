using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsBillCreatedConsumer : IConsumer<BillsBillCreatedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsBillCreatedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<BillsBillCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var members = await _db.HouseholdMembers
            .Where(x => x.HouseholdId == msg.HouseholdId && x.IsActive && x.UserId != msg.CreatedBy)
            .ToListAsync(context.CancellationToken);

        foreach (var m in members)
        {
            await _publisher.PublishAsync(new NotificationStreamEventDto(
                EventId: Guid.NewGuid(),
                RecipientUserId: m.UserId,
                EventType: "bills.bill.created",
                Title: "New bill added",
                Message: $"\"{msg.Title}\" has been added to the household",
                DeepLink: $"/bills/households/{msg.HouseholdId}/bills/{msg.BillId}",
                OccurredAt: msg.OccurredAt,
                IsRead: false), context.CancellationToken);
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsBillCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
