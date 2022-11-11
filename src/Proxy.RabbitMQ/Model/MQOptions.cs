namespace Proxy.RabbitMQ;

public class MQOptions
{
    public string? Url { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string VirtualHost { get; set; } = null!;
    public int Port { get; set; }
    public string RpcQueue { get; set; } = null!;

    public override string ToString()
    {
        return $"{nameof(User)}:{User}, {Host}://{VirtualHost}/{RpcQueue}:{Port}";
    }
}