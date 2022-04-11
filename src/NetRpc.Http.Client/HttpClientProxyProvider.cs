using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Http.Client;

public class HttpClientProxyProvider : ClientProxyProviderBase
{
    private readonly IOptionsSnapshot<HttpClientOptions> _httpClientOptions;
    private readonly IOptions<NClientOptions> _nClientOption;
    private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
    private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public HttpClientProxyProvider(IOptionsSnapshot<HttpClientOptions> httpClientOptions,
        IOptions<NClientOptions> nClientOption,
        IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
        IActionExecutingContextAccessor actionExecutingContextAccessor,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _httpClientOptions = httpClientOptions;
        _nClientOption = nClientOption;
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
            new SimpleOptions<NClientOptions>(_nClientOption.Value),
            _clientMiddlewareOptions,
            _actionExecutingContextAccessor,
            _serviceProvider,
            _loggerFactory,
            optionsName);
        return clientProxy;
    }
}