namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdMemberJoinedEvent(Guid EventId, DateTime OccurredAt, Guid MembershipId, Guid HouseholdId, Guid UserId, string Role);
