using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public class QueueStatusOptions : MQOptions
{
    public QueueStatusOptions(string host, string virtualHost, string rpcQueue, int port, string user, string password, int prefetchCount = 1) : base(
        host, virtualHost, rpcQueue, port, user, password, prefetchCount)
    {
    }

    public QueueStatusOptions(MQOptions options) : base(options.Host, options.VirtualHost, options.RpcQueue, options.Port, options.User,
        options.Password, options.PrefetchCount)
    {
    }

    public void CopyFrom(MQOptions options)
    {
        this.CopyPropertiesFrom(options);
    }

    public QueueStatusOptions()
    {
    }
}