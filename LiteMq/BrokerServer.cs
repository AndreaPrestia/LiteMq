using System.Net;
using System.Net.Sockets;
using LiteMq.Managers;

namespace LiteMq;

public class BrokerServer
{
    private readonly TcpListener _listener;
    private readonly MessageQueue _queue;

    internal BrokerServer(IPAddress ip, int port, string dbPath, SubscriptionManager subscriptionManager, PeerManager peerManager)
    {
        _listener = new TcpListener(ip, port);
        _queue = new MessageQueue(dbPath, subscriptionManager, peerManager);
    }
    
    public void Start()
    {
        _listener.Start();
        Console.WriteLine("Broker started...");
        Listen();
    }

    private async void Listen()
    {
        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
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
                        _queue.Publish(topic, parts[2], forward: false); // prevent loops
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
}