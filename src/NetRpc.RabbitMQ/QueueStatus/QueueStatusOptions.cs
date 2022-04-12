using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public class QueueStatusOptions : MQOptions
{
    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }
}