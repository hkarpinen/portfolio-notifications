using Household.Domain.Events;
using Infrastructure.Email;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class HouseholdMemberInvitedConsumer : IConsumer<HouseholdMemberInvited>
{
    private readonly NotificationsDbContext _db;
    private readonly IEmailService _email;
    private readonly string _baseUrl;

    public HouseholdMemberInvitedConsumer(NotificationsDbContext db, IEmailService email, IOptions<SmtpOptions> options)
    {
        _db = db;
        _email = email;
        _baseUrl = options.Value.BaseUrl;
    }

    public async Task Consume(ConsumeContext<HouseholdMemberInvited> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        if (!string.IsNullOrWhiteSpace(msg.RecipientEmail))
        {
            var inviter = await _db.UserEmails.FirstOrDefaultAsync(x => x.UserId == msg.InvitedByUserId, context.CancellationToken);
            var inviterName = inviter?.DisplayName ?? "Someone";

            var joinUrl = $"{_baseUrl}/household/join";
            var html = EmailTemplates.HouseholdInvite(inviterName, msg.HouseholdName, msg.InvitationCode, joinUrl);
            await _email.SendAsync(msg.RecipientEmail, msg.RecipientEmail, $"{inviterName} invited you to a household", html, context.CancellationToken);
        }

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(HouseholdMemberInvited), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
