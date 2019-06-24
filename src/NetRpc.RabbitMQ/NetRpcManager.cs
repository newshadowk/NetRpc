using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public static class NetRpcManager
    {
        public static ServiceProxy CreateServiceProxy(MQParam param, params object[] instances)
        {
            var factory = param.CreateConnectionFactory();
            return new ServiceProxy(new Service(factory, param.RpcQueue, param.PrefetchCount), instances);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(ClientConnectionFactory factory, bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(factory, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(MQParam param, bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            ClientConnectionFactory factory = new ClientConnectionFactory(param);
            return new ClientProxy<TService>(factory, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }
    }
}