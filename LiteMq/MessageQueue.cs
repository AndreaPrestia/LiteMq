using System.Net.Sockets;
using LiteDB;
using LiteMq.Managers;

namespace LiteMq;

internal class MessageQueue : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Message> _collection;
    private readonly HashSet<string> _processedHashes = [];
    private readonly SubscriptionManager _subscriptionManager;
    private readonly PeerManager _peerManager;

    public MessageQueue(string dbPath, SubscriptionManager subscriptionManager, PeerManager peerManager)
    {
        _db = new LiteDatabase(dbPath);
        _collection = _db.GetCollection<Message>("messages");
        _peerManager = peerManager;
        _subscriptionManager = subscriptionManager;
    }

    public void Publish(string topic, string payload, bool forward = true)
    {
        var hash = $"{topic}|{payload}".GetHashCode().ToString();
        if (!_processedHashes.Add(hash)) return;

        var message = new Message { Topic = topic, Payload = payload };
        _collection.Insert(message);
        _subscriptionManager.NotifyConnectedSubscribers(topic, payload);

        if (forward)
            _peerManager.ForwardToPeers(topic, payload);
    }

    public void Subscribe(string topic, TcpClient client, bool exclusive)
    {
        _subscriptionManager.Subscribe(topic, client, exclusive);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}