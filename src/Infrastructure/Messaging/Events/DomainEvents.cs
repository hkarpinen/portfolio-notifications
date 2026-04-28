namespace Infrastructure.Messaging.Events;

// Forum domain event wire shapes — must match what the forum OutboxPublisher sends.
public sealed record ForumThreadCreatedEvent(Guid ThreadId, Guid CommunityId, string CommunitySlug, Guid AuthorId, string Title, DateTime OccurredAt);
public sealed record ForumCommentCreatedEvent(Guid CommentId, Guid ThreadId, Guid AuthorId, Guid? ParentCommentId, DateTime OccurredAt);
public sealed record ForumMembershipInvitedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
public sealed record ForumModeratorAppointedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
public sealed record ForumModeratorRemovedEvent(Guid MembershipId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
public sealed record ForumUserBannedEvent(Guid BanId, Guid CommunityId, Guid UserId, string? Reason, DateTime OccurredAt);
public sealed record ForumUserUnbannedEvent(Guid BanId, Guid CommunityId, Guid UserId, DateTime OccurredAt);
public sealed record ForumThreadLockedEvent(Guid ThreadId, Guid CommunityId, DateTime OccurredAt);
public sealed record ForumCommunityOwnershipTransferredEvent(Guid CommunityId, Guid NewOwnerId, DateTime OccurredAt);

// Bills domain event wire shapes — must match what the bills OutboxPublisher sends.
public sealed record BillsHouseholdCreatedEvent(Guid HouseholdId, Guid OwnerId, DateTime OccurredAt);
public sealed record BillsHouseholdOwnershipTransferredEvent(Guid HouseholdId, Guid NewOwnerId, DateTime OccurredAt);
public sealed record BillsHouseholdMemberJoinedEvent(Guid HouseholdId, Guid UserId, DateTime OccurredAt);
public sealed record BillsHouseholdMemberLeftEvent(Guid HouseholdId, Guid UserId, DateTime OccurredAt);
public sealed record BillsHouseholdMemberRemovedEvent(Guid HouseholdId, Guid UserId, DateTime OccurredAt);
public sealed record BillsHouseholdMemberRoleChangedEvent(Guid HouseholdId, Guid UserId, string NewRole, DateTime OccurredAt);
public sealed record BillsBillCreatedEvent(Guid BillId, Guid HouseholdId, Guid CreatedBy, string Title, DateTime OccurredAt);
public sealed record BillsBillSplitCreatedEvent(Guid SplitId, Guid BillId, Guid HouseholdId, Guid UserId, DateTime OccurredAt);
