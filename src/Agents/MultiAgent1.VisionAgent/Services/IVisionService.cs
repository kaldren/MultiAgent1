using MultiAgent1.VisionAgent.Models;

namespace MultiAgent1.VisionAgent.Services;

public interface IVisionService
{
    Task<VisionAgentResponse> AnalyzeImageAsync(
        byte[] imageData,
        string contentType,
        CancellationToken cancellationToken);
}
