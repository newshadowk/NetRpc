using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public class GrpcClientProxyProvider : ClientProxyProviderBase
    {
        private readonly IOptionsMonitor<GrpcClientOptions> _grpcClientOptions;
        private readonly IOptionsMonitor<NClientOption> _nClientOption;
        private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private readonly ILoggerFactory _loggerFactory;

        public GrpcClientProxyProvider(IOptionsMonitor<GrpcClientOptions> grpcClientOptions, 
            IOptionsMonitor<NClientOption> nClientOption,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            _grpcClientOptions = grpcClientOptions;
            _nClientOption = nClientOption;
            _clientMiddlewareOptions = clientMiddlewareOptions;
            _serviceProvider = serviceProvider;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _loggerFactory = loggerFactory;
        }

        protected override ClientProxy<TService>? CreateProxyInner<TService>(string optionsName)
        {
            var options = _grpcClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new GrpcClientConnectionFactory(new SimpleOptions<GrpcClientOptions>(options), _loggerFactory);
            var clientProxy = new GrpcClientProxy<TService>(f,  
                new SimpleOptions<NClientOption>(_nClientOption.CurrentValue),
                _clientMiddlewareOptions,
                _actionExecutingContextAccessor,
                _serviceProvider,
                _loggerFactory, optionsName);
            return clientProxy;
        }
    }

    public class OrphanNGrpcClientProxyProvider: IOrphanClientProxyProvider
    {
        private readonly IOptionsMonitor<GrpcClientOptions> _grpcClientOptions;
        private readonly IOptionsMonitor<NClientOption> _nClientOption;
        private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
        private readonly ILoggerFactory _loggerFactory;

        public OrphanNGrpcClientProxyProvider(IOptionsMonitor<GrpcClientOptions> grpcClientOptions, 
            IOptionsMonitor<NClientOption> nClientOption,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            _grpcClientOptions = grpcClientOptions;
            _nClientOption = nClientOption;
            _clientMiddlewareOptions = clientMiddlewareOptions;
            _serviceProvider = serviceProvider;
            _actionExecutingContextAccessor = actionExecutingContextAccessor;
            _loggerFactory = loggerFactory;
        }

        public ClientProxy<TService>? CreateProxy<TService>(string optionsName) where TService : class 
        {
            var options = _grpcClientOptions.Get(optionsName);
            if (options.IsPropertiesDefault())
                return null;

            var f = new GrpcClientConnectionFactory(new SimpleOptions<GrpcClientOptions>(options), _loggerFactory);
            var clientProxy = new GrpcClientProxy<TService>(f, 
                new SimpleOptions<NClientOption>(_nClientOption.CurrentValue),
                _clientMiddlewareOptions,
                _actionExecutingContextAccessor,
                _serviceProvider,
                _loggerFactory, 
                optionsName);
            return clientProxy;
        }
    }
}