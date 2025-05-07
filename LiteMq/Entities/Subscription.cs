using System.Net.Sockets;

namespace LiteMq.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public TcpClient? Client { get; set; }
    public bool Exclusive { get; set; }
    public DateTime Created { get; set; }
}