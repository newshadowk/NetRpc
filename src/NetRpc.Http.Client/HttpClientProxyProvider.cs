using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Http.Client
{
    public class HttpClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<HttpClientOptions> _httpClientOptions;
        private readonly IOptionsMonitor<NClientOption> _netRpcClientOption;
        private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public HttpClientProxyProvider(IOptionsMonitor<HttpClientOptions> httpClientOptions,
            IOptionsMonitor<NClientOption> netRpcClientOption,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            _httpClientOptions = httpClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _clientMiddlewareOptions = clientMiddlewareOptions;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        protected override ClientProxy<TService>? CreateProxyInner<TService>(string optionsName)
        {
            var options = _httpClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new HttpOnceCallFactory(new SimpleOptions<HttpClientOptions>(options), _loggerFactory);
            var clientProxy = new ClientProxy<TService>(
                f,
                new SimpleOptions<NClientOption>(_netRpcClientOption.CurrentValue),
                _clientMiddlewareOptions,
                _actionExecutingContextAccessor,
                _serviceProvider,
                _loggerFactory,
                optionsName);
            return clientProxy;
        }
    }

    public class OrphanHttpClientProxyProvider : IOrphanClientProxyProvider
    {
        private readonly IOptionsMonitor<HttpClientOptions> _httpClientOptions;
        private readonly IOptionsMonitor<NClientOption> _netRpcClientOption;
        private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public OrphanHttpClientProxyProvider(IOptionsMonitor<HttpClientOptions> httpClientOptions,
            IOptionsMonitor<NClientOption> netRpcClientOption,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            _httpClientOptions = httpClientOptions;
            _netRpcClientOption = netRpcClientOption;
            _clientMiddlewareOptions = clientMiddlewareOptions;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        public ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class
        {
            var options = _httpClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new HttpOnceCallFactory(new SimpleOptions<HttpClientOptions>(options), _loggerFactory);
            var clientProxy = new ClientProxy<TService>(
                f,
                new SimpleOptions<NClientOption>(_netRpcClientOption.CurrentValue),
                _clientMiddlewareOptions,
                _actionExecutingContextAccessor,
                _serviceProvider,
                _loggerFactory,
                optionsName);
            return clientProxy;
        }
    }
}