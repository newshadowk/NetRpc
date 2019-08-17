using System;
using Microsoft.Extensions.Hosting;

namespace NetRpc.RabbitMQ
{
    public static class NetRpcManager
    {
        public static IHost CreateHost(MQOptions mqOptions, MiddlewareOptions middlewareOptions, params Type[] instanceTypes)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcRabbitMQService(i =>
                        {
                            if (mqOptions != null)
                                i.Value = mqOptions;
                        },
                        i =>
                        {
                            if (middlewareOptions != null)
                                i.Items = middlewareOptions.Items;
                        });
                    services.AddNetRpcServiceContract(instanceTypes);
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(RabbitMQClientConnectionFactoryOptions options, bool isWrapFaultException = true,
            int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(options.Factory, new SimpleOptionsMonitor<NetRpcClientOption>(new NetRpcClientOption
            {
                HearbeatInterval = hearbeatInterval,
                IsWrapFaultException = isWrapFaultException,
                TimeoutInterval = timeoutInterval
            }));
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(MQOptions options, bool isWrapFaultException = true,
            int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            var opt = new RabbitMQClientConnectionFactoryOptions(options);
            return CreateClientProxy<TService>(opt, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }
    }
}