using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MultiAgent1.Shared.Models;
using MultiAgent1.VisionAgent.Data;
using MultiAgent1.VisionAgent.Models;
using MultiAgent1.VisionAgent.Services;
using System.Text.Json;

namespace MultiAgent1.VisionAgent.Workers;

public class AgentWorker : IHostedService, IDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AgentWorker> _logger;
    private ServiceBusProcessor? _processor;

    public AgentWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        BlobServiceClient blobServiceClient,
        ILogger<AgentWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _scopeFactory = scopeFactory;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public void Dispose()
    {
        _processor?.DisposeAsync().GetAwaiter().GetResult();
        _serviceBusClient?.DisposeAsync().GetAwaiter().GetResult();
        _logger.LogInformation($"{nameof(AgentWorker)} disposed.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(AgentWorker)} started.");

        _processor = _serviceBusClient.CreateProcessor(
            "inspection-photo-uploaded",
            "vision-agent-subscription",
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1,
            });

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Processing messages...");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping worker...");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("Worker stopped.");
        }
    }

    async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            string body = args.Message.Body.ToString();
            _logger.LogInformation("Received message: {MessageId}", args.Message.MessageId);

            var blobCreatedEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (blobCreatedEvent?.Data is null)
            {
                _logger.LogWarning("Message {MessageId} has no event data; dead-lettering.", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "MissingData",
                    cancellationToken: args.CancellationToken);
                return;
            }

            string imageUrl = blobCreatedEvent.Data.Url;
            string contentType = blobCreatedEvent.Data.ContentType;

            _logger.LogInformation("Downloading image from {ImageUrl}", imageUrl);

            // Parse container + blob name from the URL so we're not hard-coded to any container name.
            // Using BlobServiceClient bypasses SAS protocol restrictions in the event URL.
            var parsedUri = new BlobUriBuilder(new Uri(imageUrl));
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(parsedUri.BlobContainerName)
                .GetBlobClient(parsedUri.BlobName);

            BlobDownloadResult download = await blobClient.DownloadContentAsync(cancellationToken: args.CancellationToken);
            byte[] imageBytes = download.Content.ToArray();

            _logger.LogInformation("Downloaded {Bytes} bytes, content-type={ContentType}", imageBytes.Length, contentType);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var visionService = scope.ServiceProvider.GetRequiredService<IVisionService>();

            var analysisResult = await visionService.AnalyzeImageAsync(
                imageBytes, contentType, args.CancellationToken);

            _logger.LogInformation("Analysis: HasDefects={HasDefects} Summary={Summary}",
                analysisResult.Result.HasDefects, analysisResult.Result.Summary);

            var dbContext = scope.ServiceProvider.GetRequiredService<VisionAgentDbContext>();

            var agent = new AgentOutput { Result = analysisResult.Result };

            dbContext.AgentOutput.Add(agent);
            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Type = "VisionAnalysisCompleted",
                Payload = JsonSerializer.Serialize(new VisionAnalysisCompletedEvent
                {
                    EventId = Guid.CreateVersion7(),
                    AgentOutputId = agent.Id,
                    OccurredAt = DateTimeOffset.UtcNow,
                    Brand = analysisResult.Result.Brand,
                    Model = analysisResult.Result.Model,
                    Year = analysisResult.Result.Year,
                    HasDefects = analysisResult.Result.HasDefects,
                    Summary = analysisResult.Result.Summary,
                })
            });
            await dbContext.SaveChangesAsync(args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation("Message {MessageId} completed.", args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}; abandoning.", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "[ServiceBus Error] Source={ErrorSource}, Entity={EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }
}
