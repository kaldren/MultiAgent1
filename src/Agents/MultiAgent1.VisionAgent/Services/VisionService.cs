using Azure.Identity;
using MultiAgent1.VisionAgent.Data;
using MultiAgent1.VisionAgent.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace MultiAgent1.VisionAgent.Services;

public class VisionService : IVisionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VisionService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public VisionService(IConfiguration configuration, ILogger<VisionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<VisionAgentResponse> AnalyzeImageAsync(
        byte[] imageData,
        string contentType,
        CancellationToken cancellationToken)
    {
        var endpoint = _configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");

        var deploymentName = _configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");

        BearerTokenPolicy tokenPolicy = new(
            new AzureCliCredential(),
            "https://ai.azure.com/.default"
        );

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ChatClient client = new(
            model: deploymentName,
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri(endpoint)
            }
        );
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var textPart = ChatMessageContentPart.CreateTextPart(
            "First, identify the vehicle in this image. Examine every visible cue — badge, grille shape, headlight design, " +
            "body proportions, bodywork details, interior trim, steering wheel, instrument cluster — to determine the brand, model, and year. " +
            "Make your best estimate even when the vehicle is partially visible or the badge is not clearly readable; " +
            "only set brand/model/year to null as a last resort when there is genuinely no usable visual information.\n\n" +
            "Then inspect the vehicle for defects.\n\n" +
            "Return a JSON object with this exact structure (replace placeholder values with your findings):\n" +
            "{\n" +
            "  \"brand\": \"<manufacturer name, e.g. Toyota>\",\n" +
            "  \"model\": \"<model name, e.g. Camry>\",\n" +
            "  \"year\": <model year as integer, e.g. 2019, or null>,\n" +
            "  \"has_defects\": <true|false>,\n" +
            "  \"defects\": [\n" +
            "    {\n" +
            "      \"type\": \"<scratch|dent|crack|rust|paint|other>\",\n" +
            "      \"severity\": \"<Minor|Major|Critical>\",\n" +
            "      \"location\": \"<part of vehicle>\",\n" +
            "      \"confidence\": <0.0–1.0>\n" +
            "    }\n" +
            "  ],\n" +
            "  \"summary\": \"<one sentence summary>\"\n" +
            "}\n" +
            "Return only the JSON object with no additional text.");
        var imagePart = ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(imageData), contentType);

        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(
        [
            new SystemChatMessage(
                "You are a vehicle identification and inspection assistant. " +
                "When given a vehicle image your primary tasks are: " +
                "(1) identify the vehicle brand, model, and approximate year using all visible visual cues — " +
                "badges, grille, headlights, taillights, body lines, interior details; " +
                "(2) detect any damage or defects. " +
                "Always attempt identification before falling back to null. " +
                "Each defect must include type, severity (Minor, Major, or Critical), location, and confidence (0.0–1.0). " +
                "Respond only with valid JSON — no markdown, no explanation."),
            new UserChatMessage(textPart, imagePart),
        ],
        new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() },
        cancellationToken: cancellationToken);

        ChatCompletion completion = result.Value;
        string json = string.Concat(completion.Content.Select(p => p.Text));

        var visionResult = JsonSerializer.Deserialize<VisionResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize vision result.");

        _logger.LogInformation("Analysis complete. HasDefects={HasDefects}", visionResult.HasDefects);

        return new VisionAgentResponse
        {
            Id = Guid.NewGuid(),
            Result = visionResult,
        };
    }
}
