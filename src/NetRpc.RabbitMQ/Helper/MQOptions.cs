using System;

namespace NetRpc.RabbitMQ;

public class MQOptions
{
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string VirtualHost { get; set; } = null!;
    public int Port { get; set; }
    public string RpcQueue { get; set; } = null!;

    /// <summary>
    /// Default value is 1.
    /// </summary>
    public int PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Default value is 0 (disabled priority), max priority, 1-255, 1-10.
    /// </summary>
    public int MaxPriority { get; set; }

    /// <summary>
    /// Default value is 1 minutes.
    /// </summary>
    //public TimeSpan FirstReplyTimeOut { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan FirstReplyTimeOut { get; set; } = TimeSpan.FromSeconds(5);

    public MQOptions(string host, string virtualHost, string rpcQueue, int port, string user, string password,
        int prefetchCount = 1, int maxPriority = 0)
    {
        User = user;
        Password = password;
        Host = host;
        VirtualHost = virtualHost;
        Port = port;
        RpcQueue = rpcQueue;
        PrefetchCount = prefetchCount;
        MaxPriority = maxPriority;
    }

    public MQOptions()
    {
    }

    public override string ToString()
    {
        return $"{nameof(User)}:{User}, {Host}://{VirtualHost}/{RpcQueue}:{Port}, {nameof(PrefetchCount)}:{PrefetchCount}";
    }
}