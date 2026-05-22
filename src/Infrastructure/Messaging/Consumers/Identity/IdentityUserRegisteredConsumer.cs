using Domain.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class IdentityUserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly NotificationsDbContext _db;

    public IdentityUserRegisteredConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var existing = await _db.UserEmails.FirstOrDefaultAsync(x => x.UserId == msg.UserId, context.CancellationToken);
        if (existing is null)
            _db.UserEmails.Add(new UserEmailProjection { UserId = msg.UserId, Email = msg.Email, DisplayName = msg.DisplayName });
        else
        {
            existing.Email = msg.Email;
            existing.DisplayName = msg.DisplayName;
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(UserRegistered), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
