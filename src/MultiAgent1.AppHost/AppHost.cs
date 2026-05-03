using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("azure-service-bus");

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    serviceBus.RunAsEmulator(p => p.WithLifetime(ContainerLifetime.Persistent));
}

serviceBus.AddServiceBusTopic("inspection-photo-uploaded")
    .WithProperties(topic =>
    {
        topic.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
        topic.RequiresDuplicateDetection = true;
    })
    .AddServiceBusSubscription("vision-agent-subscription")
    .WithProperties(subscription =>
    {
        subscription.MaxDeliveryCount = 10;
        subscription.LockDuration = TimeSpan.FromMinutes(5);
    });

var storageBuilder = builder.AddAzureStorage("storage");

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    storageBuilder.RunAsEmulator(azurite => { });
}

var blobs = storageBuilder.AddBlobContainer("blobs");

var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5432);

var postgresdb = postgres.AddDatabase("visionagent");

var migrations = builder.AddProject<Projects.MultiAgent1_MigrationService>("migrations")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

var visionAgent = builder.AddProject<Projects.MultiAgent1_VisionAgent>("vision-agent")
    .WaitFor(serviceBus)
    .WaitFor(storageBuilder)
    .WaitFor(postgres)
    .WaitFor(postgresdb)
    .WithReference(serviceBus)
    .WithReference(postgresdb)
    .WithReference(migrations)
    .WaitForCompletion(migrations);

builder.AddProject<Projects.MultiAgent1_API>("api")
    .WithReference(serviceBus)
    .WithReference(blobs)
    .WaitFor(visionAgent);

builder.AddProject<Projects.MultiAgent1_MigrationService>("multiagent1-migrationservice");

builder.AddProject<Projects.MultiAgent1_CostEstimationAgent>("multiagent1-costestimationagent");

builder.Build().Run();
