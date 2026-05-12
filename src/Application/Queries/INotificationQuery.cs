using Notifications.Application.Contracts;

namespace Notifications.Application.Queries;

public interface INotificationQuery
{
    Task<NotificationStreamDto> GetRecentAsync(Guid userId, int limit, CancellationToken ct = default);
}
