using System.Net;
using System.Net.Sockets;
using LiteMq.Managers;

namespace LiteMq;

public class BrokerServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly MessageQueue _queue;

    internal BrokerServer(IPAddress ip, int port, string dbPath, SubscriptionManager subscriptionManager, PeerManager peerManager, bool deleteStorageOnStop)
    {
        _listener = new TcpListener(ip, port);
        _queue = new MessageQueue(dbPath, subscriptionManager, peerManager, deleteStorageOnStop);
    }
    
    public void Start()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("Broker started...");
            Listen();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    private async void Listen()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            while (client.Connected)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                var parts = line.Split('|', 3);
                if (parts.Length < 2) continue;
                var command = parts[0];
                var topic = parts[1];

                switch (command.ToLower())
                {
                    case "pub":
                        if (parts.Length == 3)
                            _queue.Publish(topic, parts[2], forward: true);
                        break;
                    case "sub":
                        _queue.Subscribe(topic, client, false);
                        break;
                    case "reset":
                        _queue.Reset(topic, forward: false);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
        _queue.Dispose();
        GC.SuppressFinalize(this);
    }
}