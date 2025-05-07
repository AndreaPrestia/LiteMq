using LiteDB;

namespace LiteMq.Entities;

internal class Message
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Topic { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public string Hash => $"{Topic}|{Payload}".GetHashCode().ToString();
}