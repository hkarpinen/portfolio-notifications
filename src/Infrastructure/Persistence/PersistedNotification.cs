using System.Text.Json;

namespace Infrastructure.Persistence;

/// <summary>Persisted notification record for a single recipient.</summary>
public sealed class PersistedNotification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public JsonDocument Payload { get; private set; } = JsonDocument.Parse("{}");
    public bool Read { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PersistedNotification() { }

    public PersistedNotification(Guid id, Guid userId, string type, JsonDocument payload, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Payload = payload;
        CreatedAt = createdAt;
        Read = false;
    }

    public void MarkAsRead() => Read = true;
}
