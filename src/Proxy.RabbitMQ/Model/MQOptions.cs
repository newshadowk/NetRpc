using System;

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

    /// <summary>
    /// Default value is 0 (max value), greater than will throw MaxQueueCountException.
    /// </summary>
    public int MaxQueueCount { get; set; }

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
    public TimeSpan FirstReplyTimeOut { get; set; } = TimeSpan.FromMinutes(1);
    //public TimeSpan FirstReplyTimeOut { get; set; } = TimeSpan.FromSeconds(5);

    public override string ToString()
    {
        return $"{nameof(User)}:{User}, {Host}://{VirtualHost}/{RpcQueue}:{Port}, {nameof(PrefetchCount)}:{PrefetchCount}, " +
               $"{nameof(MaxQueueCount)}:{MaxQueueCount}, {nameof(MaxPriority)}:{MaxPriority}, {nameof(FirstReplyTimeOut)}:{FirstReplyTimeOut.TotalSeconds} seconds.";
    }
}