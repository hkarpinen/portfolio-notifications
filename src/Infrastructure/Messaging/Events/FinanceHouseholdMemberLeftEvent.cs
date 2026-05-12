namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdMemberLeftEvent(Guid EventId, DateTime OccurredAt, Guid MembershipId, Guid HouseholdId, Guid UserId);
