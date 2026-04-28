using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

// ─── Bills: HouseholdCreated → seed household-member projection ───────────────

internal sealed class BillsHouseholdCreatedConsumer : IConsumer<BillsHouseholdCreatedEvent>
{
    private readonly NotificationsDbContext _db;

    public BillsHouseholdCreatedConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<BillsHouseholdCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var existing = await _db.HouseholdMembers
            .FirstOrDefaultAsync(x => x.HouseholdId == msg.HouseholdId && x.UserId == msg.OwnerId, context.CancellationToken);
        if (existing is null)
            _db.HouseholdMembers.Add(new HouseholdMemberProjection { HouseholdId = msg.HouseholdId, UserId = msg.OwnerId, IsActive = true });

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Bills: HouseholdMemberJoined → update projection + notify owner ─────────

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

// ─── Bills: HouseholdMemberLeft → deactivate in projection ───────────────────

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

// ─── Bills: HouseholdMemberRemoved → deactivate + notify removed user ─────────

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

// ─── Bills: HouseholdMemberRoleChanged → notify affected user ────────────────

internal sealed class BillsHouseholdMemberRoleChangedConsumer : IConsumer<BillsHouseholdMemberRoleChangedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsHouseholdMemberRoleChangedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<BillsHouseholdMemberRoleChangedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "bills.member.role_changed",
            Title: "Role updated",
            Message: $"Your household role has been changed to {msg.NewRole}",
            DeepLink: $"/bills/households/{msg.HouseholdId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsHouseholdMemberRoleChangedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Bills: HouseholdOwnershipTransferred → notify new owner ─────────────────

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

// ─── Bills: BillCreated → fan-out to all active household members ─────────────

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

// ─── Bills: BillSplitCreated → notify assigned member ────────────────────────

internal sealed class BillsBillSplitCreatedConsumer : IConsumer<BillsBillSplitCreatedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public BillsBillSplitCreatedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<BillsBillSplitCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "bills.split.created",
            Title: "New bill split assigned",
            Message: "You have been assigned a bill split",
            DeepLink: $"/bills/households/{msg.HouseholdId}/bills/{msg.BillId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(BillsBillSplitCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
