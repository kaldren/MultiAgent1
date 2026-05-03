using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using MultiAgent1.VisionAgent.Data;

namespace MultiAgent1.VisionAgent.Workers;

public class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    ServiceBusClient serviceBusClient,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var sender = serviceBusClient.CreateSender("vision-analysis-completed");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VisionAgentDbContext>();

            var pending = await db.OutboxMessages
                .Where(m => m.PublishedAt == null)
                .OrderBy(m => m.Id)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var msg in pending)
            {
                try
                {
                    await sender.SendMessageAsync(new ServiceBusMessage(msg.Payload)
                    {
                        Subject = msg.Type,
                        MessageId = msg.Id.ToString(),
                    }, stoppingToken);

                    msg.PublishedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Published outbox message {Id}", msg.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                }
            }
        }
    }
}
