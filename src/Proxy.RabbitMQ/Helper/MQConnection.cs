using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Proxy.RabbitMQ;

public class MQConnection : IDisposable
{
    private readonly MQOptions _options;
    private volatile bool _disposed;

    public MQConnection(MQOptions options, int prefetchCount, ILoggerFactory factory)
    {
        _options = options;
        Logger = factory.CreateLogger("NetRpc");

        MainConnection = (IAutorecoveringConnection)options.CreateMainConnectionFactory(prefetchCount).CreateConnectionLoop(Logger);
        MainConnection.ConnectionShutdown += (_, e) => Logger.LogInformation($"Client ConnectionShutdown, {e.ReplyCode}, {e.ReplyText}");
        MainConnection.ConnectionRecoveryError += (_, e) => Logger.LogInformation($"Client ConnectionRecoveryError, {e.Exception.Message}");
        MainConnection.RecoverySucceeded += (_, _) => Logger.LogInformation("Client RecoverySucceeded");

        SubConnection = (IAutorecoveringConnection)options.CreateSubConnectionFactory().CreateConnectionLoop(Logger);
        Checker = new ExclusiveChecker(SubConnection);
        SubWatcher = new SubWatcher(Checker);

        MainChannel = MainConnection.CreateModel();
    }

    public IAutorecoveringConnection MainConnection { get; }

    public IAutorecoveringConnection SubConnection { get; }

    public int GetMainQueueCount()
    {
        try
        {
            var ok = MainChannel.QueueDeclarePassive(_options.RpcQueue);
            return (int)ok.MessageCount;
        }
        catch
        {
            return -1;
        }
    }

    public ExclusiveChecker Checker { get; }

    public ILogger Logger { get; }

    public IModel MainChannel { get; }

    public SubWatcher SubWatcher { get; }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        SubWatcher.Dispose();

        MainChannel.TryClose(Logger);
        SubConnection.TryClose(Logger);
        MainConnection.TryClose(Logger);
    }
}

public sealed class ClientConnection : MQConnection
{
    public ClientConnection(MQClientOptions options, ILoggerFactory factory) : base(options, 1, factory)
    {
        Options = options;
    }

    public MQClientOptions Options { get; }
}

public sealed class ServiceConnection : MQConnection
{
    public ServiceConnection(MQServiceOptions options, ILoggerFactory factory) : base(options, options.PrefetchCount, factory)
    {
        Options = options;
    }

    public MQServiceOptions Options { get; }
}

public class ClientConnectionCache
{
    private readonly IOptionsSnapshot<MQClientOptions>? _clientOptions;
    private readonly ILoggerFactory _factory;

    private readonly ConcurrentDictionary<string, Lazy<ClientConnection>> _clients = new(StringComparer.Ordinal);

    public ClientConnectionCache(IOptionsSnapshot<MQClientOptions> clientOptions, ILoggerFactory factory)
    {
        _clientOptions = clientOptions;
        _factory = factory;
    }
  
    public ClientConnection GetClient(string optionsName = "")
    {
        if (_clientOptions == null)
            throw new ArgumentNullException(nameof(_clientOptions));

        var opt = _clientOptions.Get(optionsName);

        var key = $"{optionsName}";
        var conn = _clients.GetOrAdd(key, new Lazy<ClientConnection>(() =>
            new ClientConnection(opt, _factory), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

        return conn;
    }

    public void Close()
    {
        foreach (var i in _clients.Values) 
            i.Value.Dispose();
    }
}