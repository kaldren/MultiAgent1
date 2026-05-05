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

var outboxTopic = serviceBus.AddServiceBusTopic("vision-analysis-completed")
    .WithProperties(topic =>
    {
        topic.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
    })
    .AddServiceBusSubscription("cost-estimation-agent-subscription")
    .WithProperties(subscription =>
    {
        subscription.MaxDeliveryCount = 10;
        subscription.LockDuration = TimeSpan.FromMinutes(5);
    });

// Azure AI Search
var existingSearchName = builder.AddParameter("existingSearchName");
var existingSearchResourceGroup = builder.AddParameter("existingSearchResourceGroup");

var search = builder.AddAzureSearch("search")
    .AsExisting(existingSearchName, existingSearchResourceGroup);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(a => a.WithBlobPort(10000));
var blobs = storage.AddBlobs("blobs");

var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5432);
//.WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("visionagent");

var migrations = builder.AddProject<Projects.MultiAgent1_MigrationService>("migrations")
    .WaitFor(postgresdb)
    .WithReference(postgresdb);

var visionAgent = builder.AddProject<Projects.MultiAgent1_VisionAgent>("vision-agent")
    .WaitFor(serviceBus)
    .WaitFor(storage)
    .WaitFor(postgres)
    .WaitFor(postgresdb)
    .WaitFor(blobs)
    .WithReference(serviceBus)
    .WithReference(postgresdb)
    .WithReference(migrations)
    .WithReference(blobs)
    .WaitForCompletion(migrations);

builder.AddProject<Projects.MultiAgent1_API>("api")
    .WaitFor(serviceBus)
    .WaitFor(blobs)
    .WaitFor(visionAgent)
    .WithReference(serviceBus)
    .WithReference(blobs);

builder.AddProject<Projects.MultiAgent1_CostEstimationAgent>("cost-estimation-agent")
    .WaitFor(visionAgent)
    .WithReference(serviceBus)
    .WithReference(search);

builder.Build().Run();
