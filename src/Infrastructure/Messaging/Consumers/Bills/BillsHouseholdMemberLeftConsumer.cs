using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsHouseholdMemberLeftConsumer : IConsumer<BillsHouseholdMemberLeftEvent>
{
    private readonly NotificationsDbContext _db;

    public BillsHouseholdMemberLeftConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<BillsHouseholdMemberLeftEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var existing = await _db.HouseholdMembers
            .FirstOrDefaultAsync(x => x.HouseholdId == msg.HouseholdId && x.UserId == msg.UserId, context.CancellationToken);
        if (existing is not null) existing.IsActive = false;

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdMemberLeftEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
