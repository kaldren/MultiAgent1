namespace MultiAgent1.VisionAgent.Models;

public sealed class VisionAnalysisCompletedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = "VisionAnalysisCompleted";
    public Guid AgentOutputId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public bool HasDefects { get; set; }
    public string Summary { get; set; } = string.Empty;
}