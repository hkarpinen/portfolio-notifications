// Wire contract for identity events consumed from RabbitMQ.
// Namespace and type names must match the domain events published by the identity service.
namespace Domain.Events;

public sealed record UserRegistered(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName);

public sealed record UserEmailConfirmationRequested(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string ConfirmationToken);

public sealed record UserPasswordResetRequested(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string ResetToken);
