using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class ForumThreadCreatedConsumer : IConsumer<ForumThreadCreatedEvent>
{
    private readonly NotificationsDbContext _db;

    public ForumThreadCreatedConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ForumThreadCreatedEvent> context)
    {
        var msg = context.Message;
        if (await IsProcessedAsync(context.MessageId ?? Guid.NewGuid(), context.CancellationToken)) return;

        var existing = await _db.ThreadAuthors.FindAsync(new object[] { msg.ThreadId }, context.CancellationToken);
        if (existing is null)
            _db.ThreadAuthors.Add(new ThreadAuthorProjection { ThreadId = msg.ThreadId, AuthorId = msg.AuthorId, CommunitySlug = msg.CommunitySlug });
        else
        {
            existing.AuthorId = msg.AuthorId;
            existing.CommunitySlug = msg.CommunitySlug;
        }

        await SaveAsync(context.MessageId ?? Guid.NewGuid(), nameof(ForumThreadCreatedEvent), context.CancellationToken);
    }

    private Task<bool> IsProcessedAsync(Guid id, CancellationToken ct)
        => _db.ProcessedEvents.AnyAsync(x => x.EventId == id, ct);

    private async Task SaveAsync(Guid id, string type, CancellationToken ct)
    {
        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = id, EventType = type, ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
