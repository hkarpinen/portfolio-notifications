using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Commands;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsBillSplitCreatedConsumer : IConsumer<FinanceExpenseSplitCreatedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsBillSplitCreatedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<FinanceExpenseSplitCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new PublishNotificationCommand(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "bills.split.created",
            Title: "New bill split assigned",
            Message: "You have been assigned a bill split",
            DeepLink: $"/bills/households/{msg.HouseholdId}/bills/{msg.ExpenseId}",
            OccurredAt: msg.OccurredAt), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(FinanceExpenseSplitCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
