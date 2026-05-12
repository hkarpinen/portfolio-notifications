namespace Infrastructure.Messaging.Events;

public sealed record FinanceExpenseCreatedEvent(Guid EventId, DateTime OccurredAt, Guid ExpenseId, Guid? HouseholdId, string Title, Guid CreatedBy);
