namespace Infrastructure.Messaging.Events;

public sealed record FinanceExpenseSplitCreatedEvent(Guid EventId, DateTime OccurredAt, Guid ExpenseSplitId, Guid ExpenseId, Guid HouseholdId, Guid MembershipId, Guid UserId);
