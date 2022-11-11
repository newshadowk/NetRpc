namespace Proxy.RabbitMQ;

/// <summary>
/// Support OptionName.
/// </summary>
public class MQClientOptions : MQOptions
{
    /// <summary>
    /// Default value is 0 (max value), greater than will throw MaxQueueCountException.
    /// </summary>
    public int MaxQueueCount { get; set; }

    /// <summary>
    /// Default value is 1 minutes.
    /// </summary>
    public TimeSpan FirstReplyTimeOut { get; set; } = TimeSpan.FromMinutes(1);

    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }
}