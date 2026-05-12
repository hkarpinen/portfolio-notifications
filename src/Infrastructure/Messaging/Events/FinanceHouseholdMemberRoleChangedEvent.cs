namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdMemberRoleChangedEvent(Guid EventId, DateTime OccurredAt, Guid MembershipId, Guid HouseholdId, Guid UserId, string NewRole);
