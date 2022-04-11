using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.RabbitMQ;

public class QueueStatusProvider : QueueStatusProviderBase
{
    private readonly IOptionsSnapshot<QueueStatusOptions> _options;
    private readonly ILoggerFactory _factory;

    public QueueStatusProvider(IOptionsSnapshot<QueueStatusOptions> options, ILoggerFactory factory)
    {
        _options = options;
        _factory = factory;
    }

    protected override QueueStatus? CreateQueueStatusInner(string optionsName)
    {
        var options = _options.Get(optionsName);
        if (options.IsPropertiesDefault())
            return null;

        return new QueueStatus(options, _factory);
    }
}

public abstract class QueueStatusProviderBase : IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<object?>> _caches = new(StringComparer.Ordinal);

    protected abstract QueueStatus? CreateQueueStatusInner(string optionsName);

    public QueueStatus? CreateQueueStatus(string optionsName)
    {
        var key = $"{optionsName}";
        var clientProxy = (QueueStatus?)_caches.GetOrAdd(key, new Lazy<object?>(() =>
            CreateQueueStatusInner(optionsName), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        return clientProxy;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            DisposeManaged();
    }

    private void DisposeManaged()
    {
        foreach (var proxy in _caches.Values)
        {
            var disposable = proxy.Value as IDisposable;
            disposable?.Dispose();
        }
    }
}