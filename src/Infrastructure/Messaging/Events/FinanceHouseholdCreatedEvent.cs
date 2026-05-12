namespace Infrastructure.Messaging.Events;

public sealed record FinanceHouseholdCreatedEvent(Guid EventId, DateTime OccurredAt, Guid HouseholdId, string Name, Guid OwnerId, string CurrencyCode);
