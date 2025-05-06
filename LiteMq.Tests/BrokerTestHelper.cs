using System.Net;
using Xunit.Abstractions;

namespace LiteMq.Tests;

public class BrokerTestHelper(ITestOutputHelper testOutputHelper)
{
    private readonly List<(Thread thread, string ipAndPort)> _runningBrokers = new();

    public void StartCluster(int[] ports)
    {
        var endpoints = new List<IPEndPoint>();
        foreach (var port in ports)
            endpoints.Add(new IPEndPoint(IPAddress.Loopback, port));

        foreach (var port in ports)
        {
            var peers = new List<IPEndPoint>(endpoints);
            peers.RemoveAll(e => e.Port == port);
            StartBroker("127.0.0.1", port, peers);
        }

        // Let them warm up
        Thread.Sleep(1000);
    }

    public void StopAll()
    {
        foreach (var (thread, ipAndPort) in _runningBrokers)
        {
            try { thread.Interrupt(); } catch(Exception ex) { testOutputHelper.WriteLine(ex.ToString()); }
            try { File.Delete(ipAndPort); } catch(Exception ex) { testOutputHelper.WriteLine(ex.ToString()); }
        }
        _runningBrokers.Clear();
    }
    
    private void StartBroker(string ip, int port, List<IPEndPoint> peers)
    {
        var thread = new Thread(() =>
        {
            var server = BrokerServerBuilder.Create().WithIp(ip).WithPort(port).WithPeers(peers).Build();
            server.Start(); // This blocks, so we use a thread
        })
        {
            IsBackground = true
        };
        thread.Start();
        _runningBrokers.Add((thread, $"{ip}:{port}"));
    }
}