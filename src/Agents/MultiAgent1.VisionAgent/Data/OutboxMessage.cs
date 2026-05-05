namespace MultiAgent1.VisionAgent.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty; // e.g., "AgentOutputCreated", "AgentOutputUpdated", etc.
    public string Payload { get; set; } = string.Empty; // JSON-serialized data relevant to the event
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; // Timestamp when the message was created
    public DateTimeOffset? PublishedAt { get; set; } // Nullable, set when the message is published to the message broker
}
