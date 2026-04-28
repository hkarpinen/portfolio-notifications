namespace Infrastructure.Persistence;

/// <summary>Projection: thread author for comment-notification routing.</summary>
public sealed class ThreadAuthorProjection
{
    public Guid ThreadId { get; set; }
    public Guid AuthorId { get; set; }
    public string CommunitySlug { get; set; } = string.Empty;
}

/// <summary>Projection: comment author for reply-notification routing.</summary>
public sealed class CommentAuthorProjection
{
    public Guid CommentId { get; set; }
    public Guid AuthorId { get; set; }
}

/// <summary>Projection: active household memberships for bill-fan-out routing.</summary>
public sealed class HouseholdMemberProjection
{
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Tracks processed MassTransit message IDs to ensure idempotent delivery.</summary>
public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
