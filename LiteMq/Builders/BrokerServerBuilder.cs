using System.Net;

namespace LiteMq;

public class BrokerServerBuilder
{
    private IPAddress? _ip;
    private int _port;
    private string? _dbPath;
    private List<IPEndPoint> _peers;

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

    public BrokerServer Build()
    {
        _ip ??= IPAddress.Loopback;

        if (_port <= 0)
        {
            _port = 80;
        }

        _dbPath = $"LiteMq_{_ip.ToString()}_{_port}.db";
        
        return new BrokerServer(_ip, _port, _dbPath, _peers);
    }
}