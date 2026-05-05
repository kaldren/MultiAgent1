using System.Text.Json.Serialization;

namespace MultiAgent1.CostEstimationAgent.Data;

public sealed class AgentOutput
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public EstimationResult Result { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EstimationResult
{
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("has_defects")]
    public bool HasDefects { get; set; }

    [JsonPropertyName("defects")]
    public List<Defect> Defects { get; set; } = [];

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

public sealed class Defect
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public DefectSeverity Severity { get; set; } = DefectSeverity.Minor;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DefectSeverity
{
    Minor,
    Major,
    Critical
}