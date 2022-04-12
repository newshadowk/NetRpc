
using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public class RabbitMQServiceOptions : MQOptions
{
    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }
}