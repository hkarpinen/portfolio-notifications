namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdMemberRemovedEvent(Guid EventId, DateTime OccurredAt, Guid MembershipId, Guid HouseholdId, Guid UserId);
