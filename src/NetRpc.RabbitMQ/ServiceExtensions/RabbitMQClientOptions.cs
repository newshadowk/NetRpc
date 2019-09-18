namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientOptions : MQOptions
    {
        public RabbitMQClientOptions(string host, string virtualHost, string rpcQueue, int port, string user, string password, int prefetchCount = 1) : base(host, virtualHost, rpcQueue, port, user, password, prefetchCount)
        {
        }

        public RabbitMQClientOptions(MQOptions options) : base(options.Host, options.VirtualHost, options.RpcQueue, options.Port, options.User, options.Password, options.PrefetchCount)
        {

        }

        public void CopyFrom(MQOptions options)
        {
            this.CopyPropertiesFrom(options);
        }

        public RabbitMQClientOptions()
        {
        }
    }
}