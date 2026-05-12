namespace Infrastructure.Messaging.Events;

public sealed record ForumModeratorAppointedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
