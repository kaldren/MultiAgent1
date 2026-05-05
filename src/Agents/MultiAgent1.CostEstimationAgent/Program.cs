using Microsoft.EntityFrameworkCore;
using MultiAgent1.CostEstimationAgent.Data;
using MultiAgent1.CostEstimationAgent.Workers;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<AgentWorker>();

builder.AddAzureServiceBusClient("azure-service-bus");

builder.AddNpgsqlDbContext<CostEstimationAgentDbContext>(connectionName: "costestimationagent", configureDbContextOptions: options =>
{
    options.UseSnakeCaseNamingConvention();
});

builder.AddAzureSearchClient(connectionName: "search");

var host = builder.Build();
host.Run();
