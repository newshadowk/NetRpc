using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class RabbitMQOnceCall : IDisposable
{
    private readonly CheckWriteOnceBlock<string> _clientToServiceQueueOnceBlock = new();
    private readonly IModel _mainChannel;
    private readonly IConnection _subConnection;
    private volatile IModel? _subChannel;
    private readonly MainWatcher _mainWatcher;
    private readonly SubWatcher _subWatcher;
    private readonly ILogger _logger;
    private readonly string _rpcQueue;
    private readonly TimeSpan _firstReplyTimeOut;
    private string _clientToServiceQueue = null!;
    private volatile bool _disposed;
    private string _serviceToClientQueue = null!;
    private bool isFirstSend = true;
    private volatile string? _consumerTag;
    private readonly AsyncLock _lock_Receive = new();
    private readonly CancellationTokenSource _firstCts = new();
    private volatile string? _firstCid;
    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>?>>? ReceivedAsync;
    public event EventHandler? Disconnected;

    public RabbitMQOnceCall(IConnection subConnection, IModel mainChannel, MainWatcher mainWatcher, SubWatcher subWatcher, string rpcQueue, TimeSpan firstReplyTimeOut, ILogger logger)
    {
        _mainChannel = mainChannel;
        _subConnection = subConnection;
        _rpcQueue = rpcQueue;
        _firstReplyTimeOut = firstReplyTimeOut;
        _logger = logger;

        _subWatcher = subWatcher;
        _subWatcher.Disconnected += SubWatcherDisconnected;

        _mainWatcher = mainWatcher;
        _mainWatcher.Disconnected += MainWatcherDisconnected;
    }

    private void MainWatcherDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("client MainWatcherDisconnected");
        OnDisconnected();
    }

    private void SubWatcherDisconnected(object? sender, EventArgsT<string> e)
    {
        _logger.LogWarning($"client SubWatcherDisconnected, {e.Value}");
        OnDisconnected();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _mainWatcher.Disconnected -= MainWatcherDisconnected;
        _subWatcher.Disconnected -= SubWatcherDisconnected;
        _mainChannel.BasicReturn -= BasicReturn;
        _subWatcher.Remove(_clientToServiceQueue);
        _subChannel?.TryBasicCancel(_consumerTag, _logger);
        _subChannel?.Close();
    }

    public void Start(bool isPost)
    {
        if (!isPost)
        {
            _mainChannel.BasicReturn += BasicReturn;
            _firstCid = Guid.NewGuid().ToString("N");
            _subChannel = _subConnection.CreateModel();
            _serviceToClientQueue = _subChannel.QueueDeclare().QueueName;
            Debug.WriteLine($"client: _serviceToClientQueue: {_serviceToClientQueue}");
            var consumer = new AsyncEventingBasicConsumer(_subChannel);
            consumer.Received += ConsumerReceivedAsync;
            _consumerTag = _subChannel.BasicConsume(_serviceToClientQueue, true, consumer);
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isPost, int mqPriority)
    {
        if (isPost)
        {
            await SendPostAsync(buffer, mqPriority);
            return;
        }

        if (isFirstSend)
        {
            await SendFirstAsync(buffer, mqPriority);
            return;
        }

        //SendAfter
        //bug: after invoke 'BasicPublish' need an other thread to publish for real send? (sometimes happened.)
        //blocking thread in OnceCall row 96:Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
        _subChannel.BasicPublish("", _clientToServiceQueue, null, buffer);
    }

    private async Task SendPostAsync(ReadOnlyMemory<byte> buffer, int mqPriority)
    {
        var p = CreateProp(mqPriority);
        _mainChannel.BasicPublish("", _rpcQueue, p, buffer);
        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(null));
    }

    private async Task SendFirstAsync(ReadOnlyMemory<byte> buffer, int mqPriority)
    {
        var p = CreateProp(mqPriority);
        p.ReplyTo = _serviceToClientQueue;
        p.CorrelationId = _firstCid;
        _mainChannel.BasicPublish("", _rpcQueue, true, p, buffer);

        try
        {
            _clientToServiceQueue = await _clientToServiceQueueOnceBlock.WriteOnceBlock.ReceiveAsync(_firstReplyTimeOut, _firstCts.Token);
        }
        catch (OperationCanceledException)
        {
            var msg = $"message has not sent to queue, check queue if exist : {_rpcQueue}.";
            _logger.LogWarning(msg);
            throw new InvalidOperationException(msg);
        }
        catch (TimeoutException)
        {
            var msg = $"wait first reply timeout, {_firstReplyTimeOut.TotalSeconds} seconds.";
            _logger.LogWarning(msg);
            throw new TimeoutException(msg);
        }

        _subWatcher.Add(_clientToServiceQueue);
        isFirstSend = false;
    }

    private async Task ConsumerReceivedAsync(object s, BasicDeliverEventArgs e)
    {
        if (!_clientToServiceQueueOnceBlock.IsPosted)
            lock (_clientToServiceQueueOnceBlock.SyncRoot)
            {
                if (!_clientToServiceQueueOnceBlock.IsPosted)
                {
                    _clientToServiceQueueOnceBlock.IsPosted = true;
                    _clientToServiceQueueOnceBlock.WriteOnceBlock.Post(Encoding.UTF8.GetString(e.Body.ToArray()));
                }
            }
        else
        {
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(e.Body));
        }
    }

    private void BasicReturn(object? sender, BasicReturnEventArgs args)
    {
        if (args.BasicProperties.CorrelationId == _firstCid)
        {
            _logger.LogInformation(
                $"cmd send to queue failed, BasicReturn, ReplyCode:{args.ReplyCode} Exchange:{args.Exchange}, RoutingKey:{args.RoutingKey}, ReplyText:{args.ReplyText}");
            _firstCts.Cancel();
        }
    }

    private async Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>?> e)
    {
        //Consumer will has 2 threads invoke simultaneously.
        //lock here make sure the msg sequence
        using (await _lock_Receive.LockAsync())
            await ReceivedAsync.InvokeAsync(this, e);
    }

    private IBasicProperties CreateProp(int mqPriority = 0)
    {
        var p = _mainChannel.CreateBasicProperties();
        if (mqPriority != 0)
            p.Priority = (byte)mqPriority;
        return p;
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }
}