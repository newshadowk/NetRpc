namespace NetRpc.Grpc
{
    public static class NetRpcManager
    {
        public static ServiceProxy CreateServiceProxy(string host, int port, params object[] instances)
        {
            return new ServiceProxy(host, port, instances);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var factory = new ClientConnectionFactory(host, port);
            return new GrpcClientProxy<TService>(factory, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(ClientConnectionFactory factory, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new GrpcClientProxy<TService>(factory, timeoutInterval, hearbeatInterval);
        }
    }
}