// Wire contract for household events consumed from RabbitMQ.
// Namespace and type names must match the domain events published by the household service.
namespace Household.Domain.Events;

public sealed record HouseholdMemberInvited(
    Guid MembershipId,
    Guid HouseholdId,
    string HouseholdName,
    Guid InvitedByUserId,
    string InvitationCode,
    string? RecipientEmail,
    DateTime InvitedAt);
