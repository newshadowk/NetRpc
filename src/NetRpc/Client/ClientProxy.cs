using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;

namespace NetRpc;

public class ClientProxy<TService> : IClientProxy<TService> where TService : class
{
    private bool _disposed;
    private readonly object _lockDispose = new();
    private readonly IOnceCallFactory _onceCallFactory;
    private readonly ConcurrentDictionary<Type, ClientRetryAttribute?> _clientRetryAttributes = new();
    private readonly ConcurrentDictionary<Type, ClientNotRetryAttribute?> _clientNotRetryAttributes = new();

    public Guid Id { get; } = Guid.NewGuid();

    public Dictionary<string, object?> AdditionContextHeader
    {
        get => Call.AdditionContextHeader;
        set => Call.AdditionContextHeader = value;
    }

    public Dictionary<string, object?> AdditionHeader { get; } = new();

    public ClientProxy(IOnceCallFactory onceCallFactory,
        IOptions<NClientOptions> nClientOptions,
        IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
        IActionExecutingContextAccessor actionExecutingContextAccessor,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        string? optionsName = null)
    {
        _onceCallFactory = onceCallFactory;
        var logger = loggerFactory.CreateLogger("NetRpc");

        var callFactory = new CallFactory(typeof(TService),
            Id,
            serviceProvider,
            clientMiddlewareOptions.Value,
            actionExecutingContextAccessor,
            onceCallFactory,
            nClientOptions.Value,
            AdditionHeader,
            optionsName);

        var invoker = new ClientMethodRetryInvoker(callFactory, GetClientRetryAttribute(), GetServiceTypeNotRetryAttribute(), logger);
        Proxy = SimpleDispatchProxyAsync.Create<TService>(invoker);
    }

    public ClientProxy(IClientConnectionFactory factory,
        IOptions<NClientOptions> nClientOptions,
        IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
        IActionExecutingContextAccessor actionExecutingContextAccessor,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        string? optionsName = null)
        : this(new OnceCallFactory(factory, loggerFactory),
            nClientOptions,
            clientMiddlewareOptions,
            actionExecutingContextAccessor,
            serviceProvider,
            loggerFactory,
            optionsName)
    {
    }

    private ClientRetryAttribute? GetClientRetryAttribute()
    {
        var ret = _clientRetryAttributes.GetOrAdd(typeof(TService),
            t => t.GetCustomAttribute<ClientRetryAttribute>(true));
        return ret;
    }

    private ClientNotRetryAttribute? GetServiceTypeNotRetryAttribute()
    {
        var ret = _clientNotRetryAttributes.GetOrAdd(typeof(TService),
            t => t.GetCustomAttribute<ClientNotRetryAttribute>(true));
        return ret;
    }

    public TService Proxy { get; }

    object IClientProxy.Proxy => Proxy;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ClientProxy()
    {
#pragma warning disable 4014
        Dispose(false);
#pragma warning restore 4014
    }

    protected void Dispose(bool disposing)
    {
        lock (_lockDispose)
        {
            if (_disposed)
                return;

            if (disposing)
                DisposeManaged();

            _disposed = true;
        }
    }

    private void DisposeManaged()
    {
        _onceCallFactory.Dispose();
    }
}