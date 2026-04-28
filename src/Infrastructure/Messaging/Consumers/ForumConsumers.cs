using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

// ─── Forum: ThreadCreated → update thread-author projection ──────────────────

internal sealed class ForumThreadCreatedConsumer : IConsumer<ForumThreadCreatedEvent>
{
    private readonly NotificationsDbContext _db;

    public ForumThreadCreatedConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ForumThreadCreatedEvent> context)
    {
        var msg = context.Message;
        if (await IsProcessedAsync(context.MessageId ?? Guid.NewGuid(), context.CancellationToken)) return;

        var existing = await _db.ThreadAuthors.FindAsync(new object[] {msg.ThreadId}, context.CancellationToken);
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

// ─── Forum: CommentCreated → update comment-author projection + notify ────────

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
        var existing = await _db.CommentAuthors.FindAsync(new object[] {msg.CommentId}, context.CancellationToken);
        if (existing is null)
            _db.CommentAuthors.Add(new CommentAuthorProjection { CommentId = msg.CommentId, AuthorId = msg.AuthorId });

        // Notify the thread author (skip self-posts).
        // Throw if the projection is missing so MassTransit retries — the
        // ThreadCreated event may not have been consumed yet (ordering race).
        var threadAuthor = await _db.ThreadAuthors.FindAsync(new object[] {msg.ThreadId}, context.CancellationToken)
            ?? throw new InvalidOperationException($"ThreadAuthor projection missing for thread {msg.ThreadId} — will retry");
        if (threadAuthor.AuthorId != msg.AuthorId)
        {
            await _publisher.PublishAsync(new NotificationStreamEventDto(
                EventId: Guid.NewGuid(),
                RecipientUserId: threadAuthor.AuthorId,
                EventType: "forum.comment.created",
                Title: "New comment on your thread",
                Message: "Someone commented on your thread",
                DeepLink: $"/communities/{threadAuthor.CommunitySlug}/threads/{msg.ThreadId}",
                OccurredAt: msg.OccurredAt,
                IsRead: false), context.CancellationToken);
        }

        // Notify the parent comment author on a reply.
        if (msg.ParentCommentId.HasValue)
        {
            var parentAuthor = await _db.CommentAuthors.FindAsync(new object[] {msg.ParentCommentId.Value}, context.CancellationToken);
            if (parentAuthor is not null
                && parentAuthor.AuthorId != msg.AuthorId
                && parentAuthor.AuthorId != threadAuthor.AuthorId)
            {
                await _publisher.PublishAsync(new NotificationStreamEventDto(
                    EventId: Guid.NewGuid(),
                    RecipientUserId: parentAuthor.AuthorId,
                    EventType: "forum.comment.reply",
                    Title: "Someone replied to your comment",
                    Message: "You have a new reply",
                    DeepLink: $"/communities/{threadAuthor.CommunitySlug}/threads/{msg.ThreadId}",
                    OccurredAt: msg.OccurredAt,
                    IsRead: false), context.CancellationToken);
            }
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumCommentCreatedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: MembershipInvited → notify invitee ───────────────────────────────

internal sealed class ForumMembershipInvitedConsumer : IConsumer<ForumMembershipInvitedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumMembershipInvitedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumMembershipInvitedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.membership.invited",
            Title: "You've been invited to a community",
            Message: "You have received a community invitation",
            DeepLink: null,
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumMembershipInvitedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: ModeratorAppointed ────────────────────────────────────────────────

internal sealed class ForumModeratorAppointedConsumer : IConsumer<ForumModeratorAppointedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumModeratorAppointedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumModeratorAppointedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.moderator.appointed",
            Title: "You are now a moderator",
            Message: "You have been appointed as a community moderator",
            DeepLink: $"/communities/{msg.CommunityId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumModeratorAppointedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: ModeratorRemoved ──────────────────────────────────────────────────

internal sealed class ForumModeratorRemovedConsumer : IConsumer<ForumModeratorRemovedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumModeratorRemovedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumModeratorRemovedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.moderator.removed",
            Title: "Moderator role removed",
            Message: "Your moderator role has been removed",
            DeepLink: $"/communities/{msg.CommunityId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumModeratorRemovedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: UserBanned ────────────────────────────────────────────────────────

internal sealed class ForumUserBannedConsumer : IConsumer<ForumUserBannedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumUserBannedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumUserBannedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var message = msg.Reason is not null
            ? $"You have been banned: {msg.Reason}"
            : "You have been banned from this community";

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.user.banned",
            Title: "Community ban",
            Message: message,
            DeepLink: null,
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumUserBannedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: UserUnbanned ──────────────────────────────────────────────────────

internal sealed class ForumUserUnbannedConsumer : IConsumer<ForumUserUnbannedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumUserUnbannedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumUserUnbannedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.user.unbanned",
            Title: "Ban lifted",
            Message: "Your community ban has been lifted",
            DeepLink: null,
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumUserUnbannedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: ThreadLocked → notify thread author ───────────────────────────────

internal sealed class ForumThreadLockedConsumer : IConsumer<ForumThreadLockedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumThreadLockedConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumThreadLockedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var threadAuthor = await _db.ThreadAuthors.FindAsync(new object[] {msg.ThreadId}, context.CancellationToken)
            ?? throw new InvalidOperationException($"ThreadAuthor projection missing for thread {msg.ThreadId} — will retry");

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: threadAuthor.AuthorId,
            EventType: "forum.thread.locked",
            Title: "Your thread was locked",
            Message: "A moderator locked your thread",
            DeepLink: $"/communities/{threadAuthor.CommunitySlug}/threads/{msg.ThreadId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumThreadLockedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}

// ─── Forum: CommunityOwnershipTransferred → notify new owner ─────────────────

internal sealed class ForumCommunityOwnershipTransferredConsumer : IConsumer<ForumCommunityOwnershipTransferredEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;

    public ForumCommunityOwnershipTransferredConsumer(NotificationsDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Consume(ConsumeContext<ForumCommunityOwnershipTransferredEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new NotificationStreamEventDto(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.NewOwnerId,
            EventType: "forum.community.ownership_transferred",
            Title: "You are now the community owner",
            Message: "Community ownership has been transferred to you",
            DeepLink: $"/communities/{msg.CommunityId}",
            OccurredAt: msg.OccurredAt,
            IsRead: false), context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumCommunityOwnershipTransferredEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
