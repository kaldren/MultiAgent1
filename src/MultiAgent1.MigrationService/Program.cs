using Microsoft.EntityFrameworkCore;
using MultiAgent1.MigrationService;
using MultiAgent1.VisionAgent.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<VisionAgentDbContext>(connectionName: "visionagent", configureDbContextOptions: options =>
{
    options.UseSnakeCaseNamingConvention();
});

var host = builder.Build();
host.Run();
