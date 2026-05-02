using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using MultiAgent1.Shared.Models;
using System.Text.Json;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

//var blobServiceClient = new BlobServiceClient(
//        new Uri("https://stoeusmultiagent1dev.blob.core.windows.net"),
//        new DefaultAzureCredential());

//builder.Services.AddSingleton(blobServiceClient);

builder.AddAzureBlobServiceClient(connectionName: "blobs");
builder.AddAzureServiceBusClient("azure-service-bus");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi();

app.UseHttpsRedirection();

// Dummy GET
app.MapGet("/hello", () => "Hello from MultiAgent1 API!");

// Send test message to Service Bus topic
app.MapPost("/send-test-message", async (ServiceBusClient serviceBusClient) =>
{
    var sender = serviceBusClient.CreateSender("inspection-photo-uploaded");

    // Dummy event payload simulating a blob created event from Azure Storage. In a real scenario, this would come from an Event Grid subscription or similar mechanism.
    var eventPayload = new BlobCreatedEvent
    {
        Topic = "DUMMY",
        Subject = "/blobServices/default/containers/inspections-images/blobs/Screenshot 2026-01-27 002021.png",
        EventType = "Microsoft.Storage.BlobCreated",
        Id = Guid.NewGuid().ToString(),
        Data = new BlobCreatedEventData
        {
            Api = "PutBlob",
            RequestId = Guid.NewGuid().ToString(),
            ETag = "0x8DEA5F1A0907FC9",
            ContentType = "image/png",
            ContentLength = 168357,
            BlobType = "BlockBlob",
            AccessTier = "Default",
            Url = "http://127.0.0.1:10000/devstoreaccount1/inspections-images/scratch.jpg",
            Sequencer = "0000000000000000000000000003CEF500000000006ccd93",
            StorageDiagnostics = new BlobStorageDiagnostics
            {
                BatchId = Guid.NewGuid().ToString()
            }
        },
        DataVersion = "",
        MetadataVersion = "1",
        EventTime = DateTimeOffset.UtcNow
    };

    var json = JsonSerializer.Serialize(eventPayload);
    var message = new ServiceBusMessage(json)
    {
        ContentType = "application/json"
    };

    await sender.SendMessageAsync(message);
    return Results.Ok(new { message = "Test message sent to Service Bus topic", eventId = eventPayload.Id });
});

// Uploads a picture of an inspection to Azure Blob Storage
app.MapPost("/upload", async (IFormFile file, BlobServiceClient blobServiceClient) =>
{
    var containerClient = blobServiceClient.GetBlobContainerClient("inspections-images");
    var blobClient = containerClient.GetBlobClient(file.FileName);

    using (var stream = file.OpenReadStream())
    {
        await blobClient.UploadAsync(stream, true);
    }

    return Results.Ok(new { file.FileName });
});

app.Run();
