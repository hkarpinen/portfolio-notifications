namespace Infrastructure.Persistence;

/// <summary>Tracks processed MassTransit message IDs to ensure idempotent delivery.</summary>
public sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
