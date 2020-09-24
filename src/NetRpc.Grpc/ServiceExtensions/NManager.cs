using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Grpc
{
    public static class NManager
    {
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
                new SimpleOptions<NClientOption>(new NClientOption
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