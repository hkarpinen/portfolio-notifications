using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Commands;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class ForumCommentCreatedConsumer : IConsumer<ForumCommentCreatedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumCommentCreatedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumCommentCreatedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        // Update projection so future replies can find this comment's author.
        var existing = await _db.CommentAuthors.FindAsync(new object[] { msg.CommentId }, context.CancellationToken);
        if (existing is null)
            _db.CommentAuthors.Add(new CommentAuthorProjection { CommentId = msg.CommentId, AuthorId = msg.AuthorId });

        // Notify the thread author (skip self-posts).
        // Throw if the projection is missing so MassTransit retries — the
        // ThreadCreated event may not have been consumed yet (ordering race).
        var threadAuthor = await _db.ThreadAuthors.FindAsync(new object[] { msg.ThreadId }, context.CancellationToken)
            ?? throw new InvalidOperationException($"ThreadAuthor projection missing for thread {msg.ThreadId} — will retry");
        if (threadAuthor.AuthorId != msg.AuthorId)
        {
            await _publisher.PublishAsync(new PublishNotificationCommand(
                EventId: Guid.NewGuid(),
                RecipientUserId: threadAuthor.AuthorId,
                EventType: "forum.comment.created",
                Title: "New comment on your thread",
                Message: "Someone commented on your thread",
                DeepLink: $"/communities/{threadAuthor.CommunitySlug}/threads/{msg.ThreadId}",
                OccurredAt: msg.OccurredAt), context.CancellationToken);
        }

        // Notify the parent comment author on a reply.
        if (msg.ParentCommentId.HasValue)
        {
            var parentAuthor = await _db.CommentAuthors.FindAsync(new object[] { msg.ParentCommentId.Value }, context.CancellationToken);
            if (parentAuthor is not null
                && parentAuthor.AuthorId != msg.AuthorId
                && parentAuthor.AuthorId != threadAuthor.AuthorId)
            {
                await _publisher.PublishAsync(new PublishNotificationCommand(
                    EventId: Guid.NewGuid(),
                    RecipientUserId: parentAuthor.AuthorId,
                    EventType: "forum.comment.reply",
                    Title: "Someone replied to your comment",
                    Message: "You have a new reply",
                    DeepLink: $"/communities/{threadAuthor.CommunitySlug}/threads/{msg.ThreadId}",
                    OccurredAt: msg.OccurredAt), context.CancellationToken);
            }
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumCommentCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
