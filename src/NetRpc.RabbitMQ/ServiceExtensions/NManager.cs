using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.RabbitMQ
{
    public static class NManager
    {
        public static IHost CreateHost(MQOptions mqOptions, MiddlewareOptions middlewareOptions, params Contract[] contracts)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNRabbitMQService(i =>
                    {
                        if (mqOptions != null)
                            i.CopyFrom(mqOptions);
                    });
                    services.AddNetRpcMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNetRpcServiceContract(contract.ContractInfo.Type, contract.InstanceType);
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(RabbitMQClientConnectionFactoryOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(options.Factory, new SimpleOptions<NClientOption>(new NClientOption
            {
                HearbeatInterval = hearbeatInterval,
                TimeoutInterval = timeoutInterval
            }), new NullOptions<ClientMiddlewareOptions>(), ActionExecutingContextAccessor.Default,  null, NullLoggerFactory.Instance);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(MQOptions options, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var opt = new RabbitMQClientConnectionFactoryOptions(options, NullLoggerFactory.Instance);
            return CreateClientProxy<TService>(opt, timeoutInterval, hearbeatInterval);
        }
    }
}