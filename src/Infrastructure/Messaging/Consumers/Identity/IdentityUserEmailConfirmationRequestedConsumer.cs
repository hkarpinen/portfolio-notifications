using Domain.Events;
using Infrastructure.Email;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class IdentityUserEmailConfirmationRequestedConsumer : IConsumer<UserEmailConfirmationRequested>
{
    private readonly NotificationsDbContext _db;
    private readonly IEmailService _email;
    private readonly string _baseUrl;

    public IdentityUserEmailConfirmationRequestedConsumer(NotificationsDbContext db, IEmailService email, IOptions<SmtpOptions> options)
    {
        _db = db;
        _email = email;
        _baseUrl = options.Value.BaseUrl;
    }

    public async Task Consume(ConsumeContext<UserEmailConfirmationRequested> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var confirmUrl = $"{_baseUrl}/confirm-email?userId={msg.UserId}&token={Uri.EscapeDataString(msg.ConfirmationToken)}";
        var html = EmailTemplates.ConfirmEmail(msg.DisplayName, confirmUrl);
        await _email.SendAsync(msg.Email, msg.DisplayName, "Confirm your email", html, context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(UserEmailConfirmationRequested), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
