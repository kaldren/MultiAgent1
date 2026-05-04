using MultiAgent1.CostEstimationAgent.Workers;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<AgentWorker>();

builder.AddAzureServiceBusClient("azure-service-bus");

var host = builder.Build();
host.Run();
