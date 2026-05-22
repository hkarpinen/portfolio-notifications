using Infrastructure.Email;
using Infrastructure.Messaging.Events;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifications.Application.Commands;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class ForumMembershipInvitedConsumer : IConsumer<ForumMembershipInvitedEvent>
{
    private readonly NotificationsDbContext _db;
    private readonly INotificationPublisher _publisher;
    private readonly IEmailService _email;
    private readonly string _baseUrl;

    public ForumMembershipInvitedConsumer(NotificationsDbContext db, INotificationPublisher publisher, IEmailService email, IOptions<SmtpOptions> options)
    {
        _db = db;
        _publisher = publisher;
        _email = email;
        _baseUrl = options.Value.BaseUrl;
    }

    public async Task Consume(ConsumeContext<ForumMembershipInvitedEvent> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        await _publisher.PublishAsync(new PublishNotificationCommand(
            EventId: Guid.NewGuid(),
            RecipientUserId: msg.UserId,
            EventType: "forum.membership.invited",
            Title: "You've been invited to a community",
            Message: "You have received a community invitation",
            DeepLink: null,
            OccurredAt: msg.OccurredAt), context.CancellationToken);

        var userEmail = await _db.UserEmails.FirstOrDefaultAsync(x => x.UserId == msg.UserId, context.CancellationToken);
        if (userEmail is not null)
        {
            var communityUrl = $"{_baseUrl}/forum";
            var html = EmailTemplates.ForumInvite("a community", communityUrl);
            await _email.SendAsync(userEmail.Email, userEmail.DisplayName, "You've been invited to a community", html, context.CancellationToken);
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(ForumMembershipInvitedEvent), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
