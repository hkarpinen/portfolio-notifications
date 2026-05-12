namespace Infrastructure.Messaging.Events;

public sealed record ForumThreadLockedEvent(Guid ThreadId, Guid CommunityId, DateTime OccurredAt);
