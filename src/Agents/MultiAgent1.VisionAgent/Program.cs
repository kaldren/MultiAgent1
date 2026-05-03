using Microsoft.EntityFrameworkCore;
using MultiAgent1.VisionAgent.Data;
using MultiAgent1.VisionAgent.Services;
using MultiAgent1.VisionAgent.Workers;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<AgentWorker>();
builder.Services.AddHostedService<OutboxWorker>();

builder.Services.AddScoped<IVisionService, VisionService>();

builder.AddAzureServiceBusClient("azure-service-bus");

builder.AddAzureBlobServiceClient("blobs");

builder.AddNpgsqlDbContext<VisionAgentDbContext>(connectionName: "visionagent", configureDbContextOptions: options =>
{
    options.UseSnakeCaseNamingConvention();
});

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
