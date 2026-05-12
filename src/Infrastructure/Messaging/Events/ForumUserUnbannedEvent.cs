namespace Infrastructure.Messaging.Events;

public sealed record ForumUserUnbannedEvent(Guid BanId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
