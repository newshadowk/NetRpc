using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection;

public class GrpcClientProxyProvider : ClientProxyProviderBase
{
    private readonly IOptionsMonitor<GrpcClientOptions> _grpcClientOptions;
    private readonly IOptions<NClientOptions> _nClientOption;
    private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
    private readonly ILoggerFactory _loggerFactory;

    public GrpcClientProxyProvider(IOptionsMonitor<GrpcClientOptions> grpcClientOptions,
        IOptions<NClientOptions> nClientOption,
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
        if (string.IsNullOrEmpty(options.Url))
            return null;

        var f = new GrpcClientConnectionFactory(Options.Options.Create(options), _loggerFactory);
        var clientProxy = new ClientProxy<TService>(f,
            Options.Options.Create(_nClientOption.Value),
            _clientMiddlewareOptions,
            _actionExecutingContextAccessor,
            _serviceProvider,
            _loggerFactory, optionsName);
        return clientProxy;
    }
}