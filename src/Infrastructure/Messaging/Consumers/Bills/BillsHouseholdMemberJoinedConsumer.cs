using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class BillsHouseholdMemberJoinedConsumer : IConsumer<BillsHouseholdMemberJoinedEvent>
{
    private readonly NotificationsDbContext _db;

    public BillsHouseholdMemberJoinedConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<BillsHouseholdMemberJoinedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        // Update projection.
        var existing = await _db.HouseholdMembers
            .FirstOrDefaultAsync(x => x.HouseholdId == msg.HouseholdId && x.UserId == msg.UserId, context.CancellationToken);
        if (existing is null)
            _db.HouseholdMembers.Add(new HouseholdMemberProjection { HouseholdId = msg.HouseholdId, UserId = msg.UserId, IsActive = true });
        else
            existing.IsActive = true;

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdMemberJoinedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
