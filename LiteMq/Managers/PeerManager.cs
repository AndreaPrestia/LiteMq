using System.Net;
using System.Net.Sockets;

namespace LiteMq.Managers;

internal class PeerManager(
    List<IPEndPoint> peerBrokers,
    int maxRetryForPeersCommunication,
    int maxDelayForPeersCommunicationInSeconds)
{
    public void ForwardToPeers(string command, string topic, string? payload)
    {
        foreach (var peer in peerBrokers)
        {
            Task.Run(async () =>
            {
                for (var attempt = 0; attempt < maxRetryForPeersCommunication; attempt++)
                {
                    try
                    {
                        using var client = new TcpClient();
                        await client.ConnectAsync(peer.Address, peer.Port);
                        var stream = client.GetStream();
                        var writer = new StreamWriter(stream) { AutoFlush = true };
                        await writer.WriteLineAsync(!string.IsNullOrWhiteSpace(payload)
                            ? $"{command}|{topic}|{payload}"
                            : $"{command}|{topic}");
                        break;
                    }
                    catch
                    {
                        await Task.Delay(maxDelayForPeersCommunicationInSeconds); // Retry delay
                    }
                }
            });
        }
    }
}