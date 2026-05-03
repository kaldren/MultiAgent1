using MultiAgent1.VisionAgent.Data;

namespace MultiAgent1.VisionAgent.Models;

public class VisionAgentResponse
{
    public Guid Id { get; set; }
    public VisionResult Result { get; set; } = new();
}
