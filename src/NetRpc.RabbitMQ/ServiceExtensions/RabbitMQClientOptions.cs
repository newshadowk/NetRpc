
using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

/// <summary>
/// Support OptionName.
/// </summary>
public class RabbitMQClientOptions : MQOptions
{
    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }
}