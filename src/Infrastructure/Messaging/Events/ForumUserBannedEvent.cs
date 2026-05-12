namespace Infrastructure.Messaging.Events;

public sealed record ForumUserBannedEvent(Guid BanId, Guid CommunityId, Guid UserId, string? Reason, DateTime OccurredAt);
