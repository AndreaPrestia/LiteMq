using System.Collections.Concurrent;
using System.Net.Sockets;
using LiteDB;
using LiteMq.Entities;
using LiteMq.Extensions;
using LiteMq.Managers;

namespace LiteMq;

internal class MessageQueue : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Message> _collection;
    private readonly HashSet<string> _processedHashes = [];
    private readonly SubscriptionManager _subscriptionManager;
    private readonly PeerManager _peerManager;
    private readonly Lock _lock = new();
    private readonly BlockingCollection<Message> _persistenceQueue = new();
    private readonly bool _deleteStorageOnStop;
    private readonly string _dbPath;

    public MessageQueue(string dbPath, SubscriptionManager subscriptionManager, PeerManager peerManager, bool deleteStorageOnStop)
    {
        _dbPath = dbPath;
        _db = new LiteDatabase(new ConnectionString
        {
            Filename = dbPath,
            Connection = ConnectionType.Shared
        });
        _collection = _db.GetCollection<Message>("messages");
        _peerManager = peerManager;
        _subscriptionManager = subscriptionManager;
        _deleteStorageOnStop = deleteStorageOnStop;
    }

    public void Publish(string topic, string payload, bool forward = true)
    {
        var normalizedTopic = topic.NormalizeString();

        var hash = $"{normalizedTopic}|{payload}".GetHashCode().ToString();
        if (!_processedHashes.Add(hash)) return;

        var message = new Message { Topic = normalizedTopic, Payload = payload };
        lock (_lock)
        {
            _collection.Insert(message);
        }
        _subscriptionManager.NotifyConnectedSubscribers(normalizedTopic, payload);

        if (forward)
            _peerManager.ForwardToPeers("pub", normalizedTopic, payload);
    }

    public void Subscribe(string topic, TcpClient client, bool exclusive)
    {
        var normalizedTopic = topic.NormalizeString();

        _subscriptionManager.Subscribe(normalizedTopic, client, exclusive);

        var nextAvailableMessage = GetNextAvailableMessage(normalizedTopic);

        if (nextAvailableMessage != null)
        {
            _subscriptionManager.NotifyConnectedSubscribers(normalizedTopic, nextAvailableMessage.Payload);
        }
    }

    public void Reset(string topic, bool forward = true)
    {
        var normalizedTopic = topic.NormalizeString();
        
        lock (_lock)
        {
            _collection.DeleteMany(x => x.Topic == normalizedTopic);
        }
        
        if(forward)
            _peerManager.ForwardToPeers("reset", normalizedTopic, null);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (_deleteStorageOnStop)
        {
            File.Delete(_dbPath);
        }
        GC.SuppressFinalize(this);
    }

    private Message? GetNextAvailableMessage(string topic)
    {
        lock (_lock)
        {
            var message = _collection.Query().Where(x => x.Topic == topic).OrderBy(x => x.Timestamp).FirstOrDefault();
            return message;
        }
    }
}