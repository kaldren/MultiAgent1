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

    public async Task<AnalysisResponse> AnalyzeImageAsync(
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
            "Analyze this vehicle inspection image and return a JSON object with the following structure:\n" +
            "{\n" +
            "  \"has_defects\": true,\n" +
            "  \"defects\": [\n" +
            "    {\n" +
            "      \"type\": \"scratch\",\n" +
            "      \"severity\": \"minor\",\n" +
            "      \"location\": \"front_left_door\",\n" +
            "      \"confidence\": 0.92\n" +
            "    }\n" +
            "  ],\n" +
            "  \"summary\": \"Minor scratch detected on front-left door.\"\n" +
            "}\n" +
            "Return only the JSON object with no additional text.");
        var imagePart = ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(imageData), contentType);

        ClientResult<ChatCompletion> result = await client.CompleteChatAsync(
        [
            new SystemChatMessage(
                "You are a vehicle inspection assistant. Analyze vehicle images and return structured JSON " +
                "describing defects found. Each defect must include type, severity (can be Minor, Major, or Critical), location, and confidence (0.0–1.0). " +
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

        return new AnalysisResponse
        {
            Id = Guid.NewGuid(),
            Result = visionResult,
        };
    }
}
