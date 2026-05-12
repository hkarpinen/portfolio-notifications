namespace Infrastructure.Persistence;

/// <summary>Projection: thread author for comment-notification routing.</summary>
public sealed class ThreadAuthorProjection
{
    public Guid ThreadId { get; set; }
    public Guid AuthorId { get; set; }
    public string CommunitySlug { get; set; } = string.Empty;
}
