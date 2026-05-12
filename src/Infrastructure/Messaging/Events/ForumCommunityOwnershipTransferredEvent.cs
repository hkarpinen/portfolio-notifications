namespace Infrastructure.Messaging.Events;

public sealed record ForumCommunityOwnershipTransferredEvent(Guid CommunityId, Guid NewOwnerId, DateTime OccurredAt);
