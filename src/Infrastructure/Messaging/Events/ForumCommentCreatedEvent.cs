namespace Infrastructure.Messaging.Events;

public sealed record ForumCommentCreatedEvent(Guid CommentId, Guid ThreadId, Guid AuthorId, Guid? ParentCommentId, DateTime OccurredAt);
