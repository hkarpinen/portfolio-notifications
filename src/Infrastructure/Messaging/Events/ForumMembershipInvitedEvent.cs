namespace Infrastructure.Messaging.Events;

public sealed record ForumMembershipInvitedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
