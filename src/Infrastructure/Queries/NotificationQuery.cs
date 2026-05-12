using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts;
using Notifications.Application.Queries;

namespace Infrastructure.Queries;

internal sealed class NotificationQuery : INotificationQuery
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NotificationsDbContext _dbContext;

    public NotificationQuery(NotificationsDbContext dbContext) => _dbContext = dbContext;

    public async Task<NotificationStreamDto> GetRecentAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var rows = await _dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);

        var items = rows
            .Select(n => Deserialize(n))
            .OfType<NotificationStreamEventDto>()
            .ToList();

        return new NotificationStreamDto(items);
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
