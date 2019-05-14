namespace NetRpc.RabbitMQ
{
    public class MQParam
    {
        public string User { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string VirtualHost { get; set; }
        public int Port { get; set; }
        public string RpcQueue { get; set; }
        public int PrefetchCount { get; set; }
        public MQParam(string host, string virtualHost, string rpcQueue, int port, string user, string password, int prefetchCount = 1)
        {
            User = user;
            Password = password;
            Host = host;
            VirtualHost = virtualHost;
            Port = port;
            RpcQueue = rpcQueue;
            PrefetchCount = prefetchCount;
        }

        public MQParam()
        {
        }

        public override string ToString()
        {
            return $"{nameof(User)}:{User}, {Host}://{VirtualHost}/{RpcQueue}:{Port}, PrefetchCount:{PrefetchCount}";
        }
    }
}