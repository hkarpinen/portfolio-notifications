namespace Infrastructure.Persistence;

/// <summary>Projection: comment author for reply-notification routing.</summary>
public sealed class CommentAuthorProjection
{
    public Guid CommentId { get; set; }
    public Guid AuthorId { get; set; }
}
