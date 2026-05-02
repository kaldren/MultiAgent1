namespace MultiAgent1.Shared.Models;

public class BlobCreatedEvent
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public BlobCreatedEventData Data { get; set; } = new();
    public string DataVersion { get; set; } = string.Empty;
    public string MetadataVersion { get; set; } = string.Empty;
    public DateTimeOffset EventTime { get; set; }
}

public class BlobCreatedEventData
{
    public string Api { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string BlobType { get; set; } = string.Empty;
    public string AccessTier { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Sequencer { get; set; } = string.Empty;
    public BlobStorageDiagnostics StorageDiagnostics { get; set; } = new();
}

public class BlobStorageDiagnostics
{
    public string BatchId { get; set; } = string.Empty;
}