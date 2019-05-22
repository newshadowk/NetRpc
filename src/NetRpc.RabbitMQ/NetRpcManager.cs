using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public static class NetRpcManager
    {
        public static ServiceProxy CreateServiceProxy(MQParam param, params object[] instances)
        {
            var factory = param.CreateConnectionFactory();
            return new ServiceProxy(new Service(factory, param.RpcQueue, param.PrefetchCount), false, instances);
        }

        public static ServiceProxy CreateServiceProxy(MQParam param, bool isWrapFaultException, params object[] instances)
        {
            var factory = param.CreateConnectionFactory();
            return new ServiceProxy(new Service(factory, param.RpcQueue, param.PrefetchCount), isWrapFaultException, instances);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(ClientConnectionFactory factory, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(factory, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(MQParam param, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            ClientConnectionFactory factory = new ClientConnectionFactory(param);
            return new ClientProxy<TService>(factory, timeoutInterval, hearbeatInterval);
        }
    }
}
