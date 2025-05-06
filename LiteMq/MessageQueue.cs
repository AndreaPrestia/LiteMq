using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LiteDB;

namespace LiteMq;

internal class MessageQueue : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Message> _collection;
    private readonly ConcurrentDictionary<string, List<TcpClient>> _subscribers = new();
    private readonly List<IPEndPoint> _peerBrokers;
    private readonly HashSet<string> _processedHashes = new();

    public MessageQueue(string dbPath, List<IPEndPoint>? peerBrokers = null)
    {
        _db = new LiteDatabase(dbPath);
        _collection = _db.GetCollection<Message>("messages");
        _peerBrokers = peerBrokers ?? [];
    }

    public void Publish(string topic, string payload, bool forward = true)
    {
        var hash = ($"{topic}|{payload}").GetHashCode().ToString();
        if (!_processedHashes.Add(hash)) return;

        var message = new Message { Topic = topic, Payload = payload };
        _collection.Insert(message);
        NotifySubscribers(topic, payload);

        if (forward)
            ForwardToPeers(topic, payload);
    }

    public void Subscribe(string topic, TcpClient client)
    {
        if (!_subscribers.ContainsKey(topic))
            _subscribers[topic] = new List<TcpClient>();
        _subscribers[topic].Add(client);
    }

    private void NotifySubscribers(string topic, string payload)
    {
        if (_subscribers.TryGetValue(topic, out var clients))
        {
            foreach (var client in clients.ToList())
            {
                try
                {
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        var data = Encoding.UTF8.GetBytes(payload + "\n");
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch
                {
                    clients.Remove(client);
                }
            }
        }
    }

    private void ForwardToPeers(string topic, string payload)
    {
        foreach (var peer in _peerBrokers)
        {
            Task.Run(async () =>
            {
                var retryCount = 3;
                for (var attempt = 0; attempt < retryCount; attempt++)
                {
                    try
                    {
                        using var client = new TcpClient();
                        await client.ConnectAsync(peer.Address, peer.Port);
                        var stream = client.GetStream();
                        var writer = new StreamWriter(stream) { AutoFlush = true };
                        await writer.WriteLineAsync($"pub|{topic}|{payload}");
                        break;
                    }
                    catch
                    {
                        await Task.Delay(100); // Retry delay
                    }
                }
            });
        }
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}