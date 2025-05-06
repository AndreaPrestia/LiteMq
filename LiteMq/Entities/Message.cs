using LiteDB;

namespace LiteMq;

internal class Message
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Topic { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Hash => $"{Topic}|{Payload}".GetHashCode().ToString();
}