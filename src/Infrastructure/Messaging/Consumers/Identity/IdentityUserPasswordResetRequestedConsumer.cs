using Domain.Events;
using Infrastructure.Email;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifications.Application.Services;
using Npgsql;

namespace Infrastructure.Messaging.Consumers;

internal sealed class IdentityUserPasswordResetRequestedConsumer : IConsumer<UserPasswordResetRequested>
{
    private readonly NotificationsDbContext _db;
    private readonly IEmailService _email;
    private readonly string _baseUrl;

    public IdentityUserPasswordResetRequestedConsumer(NotificationsDbContext db, IEmailService email, IOptions<SmtpOptions> options)
    {
        _db = db;
        _email = email;
        _baseUrl = options.Value.BaseUrl;
    }

    public async Task Consume(ConsumeContext<UserPasswordResetRequested> context)
    {
        var msg = context.Message;
        var msgId = context.MessageId ?? Guid.NewGuid();
        if (await _db.ProcessedEvents.AnyAsync(x => x.EventId == msgId, context.CancellationToken)) return;

        var resetUrl = $"{_baseUrl}/reset-password?userId={msg.UserId}&token={Uri.EscapeDataString(msg.ResetToken)}";
        var html = EmailTemplates.PasswordReset(msg.DisplayName, resetUrl);
        await _email.SendAsync(msg.Email, msg.DisplayName, "Reset your password", html, context.CancellationToken);

        _db.ProcessedEvents.Add(new ProcessedEvent { EventId = msgId, EventType = nameof(UserPasswordResetRequested), ProcessedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(context.CancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { }
    }
}
