using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Proxy.RabbitMQ;

public class MQConnection : IDisposable
{
    private readonly MQOptions _options;
    private volatile bool _disposed;
    private readonly IAutorecoveringConnection _checkConnection;

    public MQConnection(MQOptions options, int prefetchCount, ILoggerFactory factory)
    {
        _options = options;
        Logger = factory.CreateLogger("NetRpc");

        Connection = (IAutorecoveringConnection)options.CreateMainConnectionFactory(prefetchCount).CreateConnectionLoop(Logger);
        Connection.ConnectionShutdown += (_, e) => Logger.LogInformation($"Main ConnectionShutdown, {e.ReplyCode}, {e.ReplyText}");
        Connection.ConnectionRecoveryError += (_, e) => Logger.LogInformation($"Main ConnectionRecoveryError, {e.Exception.Message}");
        Connection.RecoverySucceeded += (_, _) => Logger.LogInformation("Main RecoverySucceeded");

        _checkConnection = (IAutorecoveringConnection)options.CreateCheckerConnectionFactory().CreateConnectionLoop(Logger);
        Checker = new ChannelChecker(_checkConnection);
        SubWatcher = new SubWatcher(Checker);

        MainChannel = Connection.CreateModel();
    }

    public IAutorecoveringConnection Connection { get; }

    public int GetMainQueueCount()
    {
        try
        {
            return (int)MainChannel.MessageCount(_options.RpcQueue);
        }
        catch
        {
            return -1;
        }
    }

    public ChannelChecker Checker { get; }

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
        _checkConnection.TryClose(Logger);
        Connection.TryClose(Logger);
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
        MainChannel.BasicQos(0, (ushort)options.PrefetchCount, false);
    }

    public MQServiceOptions Options { get; }
}

public class ClientConnectionCache
{
    private readonly IOptionsMonitor<MQClientOptions>? _clientOptions;
    private readonly ILoggerFactory _factory;

    private readonly ConcurrentDictionary<string, Lazy<ClientConnection>> _clients = new(StringComparer.Ordinal);

    public ClientConnectionCache(IOptionsMonitor<MQClientOptions> clientOptions, ILoggerFactory factory)
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