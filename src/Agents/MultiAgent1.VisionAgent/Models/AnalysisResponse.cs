using MultiAgent1.VisionAgent.Data;

namespace MultiAgent1.VisionAgent.Models;

public class AnalysisResponse
{
    public Guid Id { get; set; }
    public VisionResult Result { get; set; } = new();
}
