namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdOwnershipTransferredEvent(Guid EventId, DateTime OccurredAt, Guid HouseholdId, Guid NewOwnerId);
