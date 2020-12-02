using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.RabbitMQ
{
    public static class NManager
    {
        public static IHost CreateHost(MQOptions? mqOptions, MiddlewareOptions? middlewareOptions, params ContractParam[] contracts)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNRabbitMQService(i =>
                    {
                        if (mqOptions != null)
                            i.CopyFrom(mqOptions);
                    });
                    services.AddNMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNServiceContract(contract.ContractType, contract.InstanceType!);
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(RabbitMQClientConnectionFactoryOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000) where TService : class
        {
            return new(options.Factory, new SimpleOptions<NClientOption>(new NClientOption
            {
                HearbeatInterval = hearbeatInterval,
                TimeoutInterval = timeoutInterval
            }), new NullOptions<ClientMiddlewareOptions>(), ActionExecutingContextAccessor.Default, null!, NullLoggerFactory.Instance);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(MQOptions options, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
            where TService : class
        {
            var opt = new RabbitMQClientConnectionFactoryOptions(options, NullLoggerFactory.Instance);
            return CreateClientProxy<TService>(opt, timeoutInterval, hearbeatInterval);
        }
    }
}