
namespace Proxy.RabbitMQ;

public class MQServiceOptions : MQOptions
{
    /// <summary>
    /// Default value is 1.
    /// </summary>
    public int PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Default value is 0 (disabled priority), max priority, 1-255, 1-10.
    /// </summary>
    public int MaxPriority { get; set; }

    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }
}