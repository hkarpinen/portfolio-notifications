namespace Infrastructure.Messaging.Events;

public sealed record ForumThreadCreatedEvent(Guid ThreadId, Guid CommunityId, string CommunitySlug, Guid AuthorId, string Title, DateTime OccurredAt);
