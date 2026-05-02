using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MultiAgent1.VisionAgent.Data;

// Result JSON:
// {
//   "has_defects": true,
//   "defects": [
//     {
//       "type": "scratch",
//       "severity": "minor",
//       "location": "front_left_door",
//       "confidence": 0.92
//     }
//   ],
//   "summary": "Minor scratch detected on front-left door."
// }

public sealed class AgentOutput
{
    [Key]
    public int Id { get; set; }

    [Required]
    public VisionResult Result { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VisionResult
{
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