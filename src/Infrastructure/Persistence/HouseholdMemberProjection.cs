namespace Infrastructure.Persistence;

/// <summary>Projection: active household memberships for bill-fan-out routing.</summary>
public sealed class HouseholdMemberProjection
{
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
}
