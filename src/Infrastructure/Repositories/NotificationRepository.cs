using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Services;

namespace Infrastructure.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NotificationsDbContext _dbContext;

    public NotificationRepository(NotificationsDbContext dbContext) => _dbContext = dbContext;

    public async Task PersistAsync(NotificationStreamEventDto dto, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToDocument(dto, JsonOpts);
        var entity = new PersistedNotification(dto.EventId, dto.RecipientUserId, dto.EventType, payload, dto.OccurredAt);
        await _dbContext.Notifications.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationStreamEventDto>> GetRecentAsync(
        Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.Select(Deserialize).OfType<NotificationStreamEventDto>().ToList();
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (entity is null) return;
        entity.MarkAsRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true), cancellationToken);
    }

    private static NotificationStreamEventDto? Deserialize(PersistedNotification n)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<NotificationStreamEventDto>(n.Payload.RootElement.GetRawText(), JsonOpts);
            return dto is null ? null : dto with { IsRead = n.Read };
        }
        catch { return null; }
    }
}
