using System.Net.Sockets;
using Xunit.Abstractions;

namespace LiteMq.Tests;

public class BrokerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task PublishSubscribeIntegrationTest()
    {
        var helper = new BrokerTestHelper(testOutputHelper);
        helper.StartCluster(
            [
                5000
            ]
        );

        var clientSub = new TcpClient();
        await clientSub.ConnectAsync("127.0.0.1", 5000);
        var streamSub = clientSub.GetStream();
        var writerSub = new StreamWriter(streamSub) { AutoFlush = true };
        var readerSub = new StreamReader(streamSub);

        await writerSub.WriteLineAsync("sub|test");

        var clientPub = new TcpClient();
        await clientPub.ConnectAsync("127.0.0.1", 5000);
        var streamPub = clientPub.GetStream();
        var writerPub = new StreamWriter(streamPub) { AutoFlush = true };

        await writerPub.WriteLineAsync("reset|test");

        await writerPub.WriteLineAsync("pub|test|hello world");

        var received = await readerSub.ReadLineAsync();
        Assert.Equal("hello world", received);

        clientPub.Close();
        clientSub.Close();

        helper.StopAll();
    }

    [Theory]
    [InlineData(10)]
    public async Task PublishSubscribe_ShouldMaintain_Order_Test(int messages)
    {
        var helper = new BrokerTestHelper(testOutputHelper);
        helper.StartCluster(
            [
                5000
            ]
        );

        var sentMessages = Enumerable.Range(0, messages).Select(i => $"Hello world {i}").ToList();
        var receivedMessages = new List<string>();

        var clientSub = new TcpClient();
        await clientSub.ConnectAsync("127.0.0.1", 5000);
        var streamSub = clientSub.GetStream();
        var writerSub = new StreamWriter(streamSub) { AutoFlush = true };
        var readerSub = new StreamReader(streamSub);

        await writerSub.WriteLineAsync("sub|test");

        var clientPub = new TcpClient();
        await clientPub.ConnectAsync("127.0.0.1", 5000);
        var streamPub = clientPub.GetStream();
        var writerPub = new StreamWriter(streamPub) { AutoFlush = true };

        await writerPub.WriteLineAsync("reset|test");

        foreach (var sentMessage in sentMessages)
        {
            await writerPub.WriteLineAsync($"pub|test|{sentMessage}");
        }

        while (receivedMessages.Count < sentMessages.Count)
        {
            var received = await readerSub.ReadLineAsync();
            if (!string.IsNullOrEmpty(received))
                receivedMessages.Add(received);
        }

        clientPub.Close();
        clientSub.Close();

        helper.StopAll();

        Assert.Equal(sentMessages, receivedMessages);
    }

    [Fact]
    public async Task MessageShouldForwardAcrossBrokers()
    {
        var helper = new BrokerTestHelper(testOutputHelper);
        helper.StartCluster(
            [
                5000,
                5001
            ]
        );

        var clientSub = new TcpClient();
        await clientSub.ConnectAsync("127.0.0.1", 5001);
        var streamSub = clientSub.GetStream();
        var writerSub = new StreamWriter(streamSub) { AutoFlush = true };
        var readerSub = new StreamReader(streamSub);
        await writerSub.WriteLineAsync("sub|cluster");

        var clientPub = new TcpClient();
        await clientPub.ConnectAsync("127.0.0.1", 5000);
        var streamPub = clientPub.GetStream();
        var writerPub = new StreamWriter(streamPub) { AutoFlush = true };
        await writerPub.WriteLineAsync("pub|cluster|message to cluster");

        var received = await readerSub.ReadLineAsync();
        Assert.Equal("message to cluster", received);

        clientPub.Close();
        clientSub.Close();

        helper.StopAll();
    }

    [Fact]
    public async Task DuplicateMessageShouldBeIgnored()
    {
        var helper = new BrokerTestHelper(testOutputHelper);
        helper.StartCluster([
                5000,
                5002
            ]
        );

        var clientSub = new TcpClient();
        await clientSub.ConnectAsync("127.0.0.1", 5002);
        var streamSub = clientSub.GetStream();
        var writerSub = new StreamWriter(streamSub) { AutoFlush = true };
        var readerSub = new StreamReader(streamSub);
        await writerSub.WriteLineAsync("sub|dupe");

        var clientPub = new TcpClient();
        await clientPub.ConnectAsync("127.0.0.1", 5000);
        var streamPub = clientPub.GetStream();
        var writerPub = new StreamWriter(streamPub) { AutoFlush = true };
        await writerPub.WriteLineAsync("pub|dupe|only once");
        await writerPub.WriteLineAsync("pub|dupe|only once");

        var received = await readerSub.ReadLineAsync();
        Assert.Equal("only once", received);

        var secondTask = readerSub.ReadLineAsync();
        var completed = await Task.WhenAny(secondTask, Task.Delay(1000));
        Assert.NotEqual(secondTask, completed);

        clientPub.Close();
        clientSub.Close();

        helper.StopAll();
    }
}