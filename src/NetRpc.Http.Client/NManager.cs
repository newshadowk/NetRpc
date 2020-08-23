using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Http.Client
{
    public static class NManager
    {
        public static ClientProxy<TService> CreateClientProxy<TService>(HttpClientOptions options, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
            where TService : class
        {
            return new ClientProxy<TService>(new HttpOnceCallFactory(
                    new SimpleOptions<HttpClientOptions>(options), NullLoggerFactory.Instance),
                new SimpleOptions<NClientOption>(new NClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ),
                new NullOptions<ClientMiddlewareOptions>(),
                ActionExecutingContextAccessor.Default,
                null!,
                NullLoggerFactory.Instance);
        }
    }
}