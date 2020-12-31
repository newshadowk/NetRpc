using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Grpc
{
    public static class NManager
    {
        public static IHost CreateHost(int port, MiddlewareOptions? middlewareOptions, params ContractParam[] contracts)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNMiddleware(i =>
                            {
                                if (middlewareOptions != null)
                                    i.AddItems(middlewareOptions.GetItems());
                            });
                            foreach (var contract in contracts)
                                services.AddNServiceContract(contract.ContractType, contract.InstanceType!);
                        }).Configure(app => { app.UseNGrpc(); });
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000) where TService : class
        {
            var opt = new GrpcClientConnectionFactoryOptions(options);
            return CreateClientProxy<TService>(opt, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientConnectionFactoryOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000) where TService : class
        {
            return new GrpcClientProxy<TService>(options.Factory,
                new SimpleOptions<NClientOptions>(new NClientOptions
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), new NullOptions<ClientMiddlewareOptions>(),
                ActionExecutingContextAccessor.Default,
                null!,
                NullLoggerFactory.Instance);
        }
    }
}