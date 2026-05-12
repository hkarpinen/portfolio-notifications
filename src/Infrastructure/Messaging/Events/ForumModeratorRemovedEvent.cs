namespace Infrastructure.Messaging.Events;

public sealed record ForumModeratorRemovedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
