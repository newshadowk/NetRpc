using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Proxy.RabbitMQ;

public sealed class MQConnection : IDisposable
{
    private volatile bool _disposed;

    public MQConnection(MQOptions options, bool mainWatcherEnabled, ILoggerFactory factory)
    {
        Logger = factory.CreateLogger("NetRpc");

        Options = options;

        MainConnection = (IAutorecoveringConnection)options.CreateConnectionFactory().CreateConnectionLoop(Logger);
        MainConnection.ConnectionShutdown += (_, e) => Logger.LogInformation($"Client ConnectionShutdown, {e.ReplyCode}, {e.ReplyText}");
        MainConnection.ConnectionRecoveryError += (_, e) => Logger.LogInformation($"Client ConnectionRecoveryError, {e.Exception.Message}");
        MainConnection.RecoverySucceeded += (_, _) => Logger.LogInformation("Client RecoverySucceeded");

        SubConnection = (IAutorecoveringConnection)options.CreateConnectionFactory_TopologyRecovery_Disabled().CreateConnectionLoop(Logger);
        Checker = new ExclusiveChecker(SubConnection);
        SubWatcher = new SubWatcher(Checker);
        if (mainWatcherEnabled)
            MainWatcher = new(MainConnection, options.RpcQueue);

        MainChannel = MainConnection.CreateModel();
    }

    public IAutorecoveringConnection MainConnection { get; }

    public IAutorecoveringConnection SubConnection { get; }

    public ExclusiveChecker Checker { get; }

    public ILogger Logger { get; }

    public MQOptions Options { get; }

    public IModel MainChannel { get; }

    public SubWatcher SubWatcher { get; }

    public MainWatcher? MainWatcher { get; }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        SubWatcher.Dispose();
        MainWatcher?.Dispose();

        MainChannel.TryClose(Logger);
        SubConnection.TryClose(Logger);
        MainConnection.TryClose(Logger);
    }
}