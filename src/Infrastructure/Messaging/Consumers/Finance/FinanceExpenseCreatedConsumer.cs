using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Commands;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class FinanceExpenseCreatedConsumer : IConsumer<FinanceExpenseCreatedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public FinanceExpenseCreatedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<FinanceExpenseCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var members = await _db.HouseholdMembers
            .Where(x => x.HouseholdId == msg.HouseholdId && x.IsActive && x.UserId != msg.CreatedBy)
            .ToListAsync(context.CancellationToken);

        foreach (var m in members)
        {
            await _publisher.PublishAsync(new PublishNotificationCommand(
                EventId: Guid.NewGuid(),
                RecipientUserId: m.UserId,
                EventType: "finance.expense.created",
                Title: "New expense added",
                Message: $"\"{msg.Title}\" has been added to the household",
                DeepLink: $"/households/{msg.HouseholdId}/expenses/{msg.ExpenseId}",
                OccurredAt: msg.OccurredAt), context.CancellationToken);
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(FinanceExpenseCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
