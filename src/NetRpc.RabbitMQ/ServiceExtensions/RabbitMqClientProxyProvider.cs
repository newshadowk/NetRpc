using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientProxyProvider : ClientProxyProviderBase
{
    private readonly IOptionsSnapshot<RabbitMQClientOptions> _rabbitMQClientOptions;
    private readonly IOptions<NClientOptions> _nClientOption;
    private readonly IOptions<ClientMiddlewareOptions> _clientMiddlewareOptions;
    private readonly IActionExecutingContextAccessor _actionExecutingContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public RabbitMQClientProxyProvider(IOptionsSnapshot<RabbitMQClientOptions> rabbitMQClientOptions,
        IOptions<NClientOptions> nClientOption,
        IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
        IActionExecutingContextAccessor actionExecutingContextAccessor,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _rabbitMQClientOptions = rabbitMQClientOptions;
        _nClientOption = nClientOption;
        _clientMiddlewareOptions = clientMiddlewareOptions;
        _actionExecutingContextAccessor = actionExecutingContextAccessor;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    protected override ClientProxy<TService>? CreateProxyInner<TService>(string optionsName)
    {
        var options = _rabbitMQClientOptions.Get(optionsName);
        if (options.IsPropertiesDefault())
            return null;

        var f = new RabbitMQClientConnectionFactory(new MQConnection(options, true, _loggerFactory));
        var clientProxy = new ClientProxy<TService>(
            f,
            new SimpleOptions<NClientOptions>(_nClientOption.Value),
            _clientMiddlewareOptions,
            _actionExecutingContextAccessor,
            _serviceProvider,
            _loggerFactory);
        return clientProxy;
    }
}