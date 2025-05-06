using System.Net.Sockets;
using Xunit.Abstractions;

namespace LiteMq.Tests;

public class MultiBrokerClusterTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task MessageShouldFlowThroughAllBrokers()
    {
        // Start 3 brokers (in memory)
        var helper = new BrokerTestHelper(testOutputHelper);
        helper.StartCluster([
                6000, 
                6001, 
                6002
            ]
        );

        // Allow time for startup
        await Task.Delay(1000);

        // Connect subscriber to broker 6002
        var clientSub = new TcpClient();
        await clientSub.ConnectAsync("127.0.0.1", 6002);
        var streamSub = clientSub.GetStream();
        var writerSub = new StreamWriter(streamSub) { AutoFlush = true };
        var readerSub = new StreamReader(streamSub);
        await writerSub.WriteLineAsync("sub|global");

        // Connect publisher to broker 6000
        var clientPub = new TcpClient();
        await clientPub.ConnectAsync("127.0.0.1", 6000);
        var streamPub = clientPub.GetStream();
        var writerPub = new StreamWriter(streamPub) { AutoFlush = true };
        await writerPub.WriteLineAsync("pub|global|hello cluster");

        // Read response from subscriber
        var received = await readerSub.ReadLineAsync();
        Assert.Equal("hello cluster", received);

        clientPub.Close();
        clientSub.Close();
        
        helper.StopAll();
    }
}