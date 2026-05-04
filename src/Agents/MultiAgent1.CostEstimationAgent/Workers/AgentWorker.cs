using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace MultiAgent1.CostEstimationAgent.Workers;

public class AgentWorker : IHostedService, IDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentWorker> _logger;
    private ServiceBusProcessor? _processor;

    public AgentWorker(
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        ILogger<AgentWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _scopeFactory = scopeFactory;
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
            "vision-analysis-completed",
            "cost-estimation-agent-subscription",
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
            _logger.LogInformation("CostEstimationAgent: Received message: {MessageId}", args.Message.MessageId);

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
