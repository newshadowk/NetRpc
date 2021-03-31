namespace NetRpc.RabbitMQ
{
    public class MQOptions
    {
        public string User { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Host { get; set; } = null!;
        public string VirtualHost { get; set; } = null!;
        public int Port { get; set; }
        public string RpcQueue { get; set; } = null!;
        public int PrefetchCount { get; set; }
        public bool Durable { get; set;}
        public bool AutoDelete { get; set;}


        public MQOptions(string host, string virtualHost, string rpcQueue, int port, string user, string password, int prefetchCount = 1, bool durable = false,
            bool autoDelete = true)
        {
            User = user;
            Password = password;
            Host = host;
            VirtualHost = virtualHost;
            Port = port;
            RpcQueue = rpcQueue;
            PrefetchCount = prefetchCount;
            Durable = durable;
            AutoDelete = autoDelete;
        }

        public MQOptions()
        {
        }

        public override string ToString()
        {
            return $"{nameof(User)}:{User}, {Host}://{VirtualHost}/{RpcQueue}:{Port}, {nameof(PrefetchCount)}:{PrefetchCount}, {nameof(Durable)}:{Durable}, {nameof(AutoDelete)}:{AutoDelete}";
        }
    }
}