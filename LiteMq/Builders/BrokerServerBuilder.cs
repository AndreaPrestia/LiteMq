using System.Net;
using LiteMq.Managers;

namespace LiteMq.Builders;

public class BrokerServerBuilder
{
    private IPAddress? _ip;
    private int _port;
    private string? _dbPath;
    private List<IPEndPoint> _peers;
    private int _maxRetryForPeersCommunication;
    private int _maxDelayForPeersCommunicationInSeconds;
    private bool _deleteStorageOnStop;

    private BrokerServerBuilder()
    {
        _peers = [];
    }

    public static BrokerServerBuilder Create()
    {
        return new();
    }

    public BrokerServerBuilder WithIp(string ip)
    {
        _ip = IPAddress.Parse(ip);
        return this;
    }

    public BrokerServerBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public BrokerServerBuilder WithPeers(List<IPEndPoint> peers)
    {
        _peers = peers;
        return this;
    }

    public BrokerServerBuilder WithMaxRetryForPeersCommunication(int maxRetryForPeersCommunication)
    {
        _maxRetryForPeersCommunication = maxRetryForPeersCommunication;
        return this;
    }

    public BrokerServerBuilder WithMaxDelayForPeersCommunication(int maxDelayForPeersCommunicationInSeconds)
    {
        _maxDelayForPeersCommunicationInSeconds  = maxDelayForPeersCommunicationInSeconds;
        return this;
    }
    
    public BrokerServerBuilder WithDeleteStorageOnStop(bool deleteStorageOnStop)
    {
        _deleteStorageOnStop  = deleteStorageOnStop;
        return this;
    }
    
    public BrokerServer Build()
    {
        _ip ??= IPAddress.Loopback;

        if (_port <= 0)
        {
            _port = 6000;
        }

        if (_maxRetryForPeersCommunication < 0)
        {
            _maxRetryForPeersCommunication = 0;
        }

        if (_maxDelayForPeersCommunicationInSeconds <= 0)
        {
            _maxDelayForPeersCommunicationInSeconds = 100;
        }

        _dbPath = $"LiteMq_{_ip.ToString()}_{_port}.db";
        
        return new BrokerServer(_ip, _port, _dbPath, new SubscriptionManager(), new PeerManager(_peers, _maxRetryForPeersCommunication, _maxDelayForPeersCommunicationInSeconds), _deleteStorageOnStop);
    }
}