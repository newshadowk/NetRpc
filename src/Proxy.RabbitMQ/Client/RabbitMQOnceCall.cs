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
    private volatile IModel? _subChannel;
    private readonly ILogger _logger;
    private readonly MQConnection _conn;
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

    public RabbitMQOnceCall(MQConnection conn)
    {
        _conn = conn;
        _logger = conn.Logger;

        _conn.SubWatcher.Disconnected += SubWatcherDisconnected;
    }

    private async void SubWatcherDisconnected(object? sender, EventArgsT<string> e)
    {
        if (e.Value != _clientToServiceQueue)
            return;

        if (_disposed)
            return;

        //make sure queue msg received. -- todo hack
        await Task.Delay(5000);

        if (_disposed)
            return;

        _logger.LogWarning($"client SubWatcherDisconnected, {e.Value}");
        OnDisconnected();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _conn.SubWatcher.Disconnected -= SubWatcherDisconnected;
        _conn.MainChannel.BasicReturn -= BasicReturn;
        _conn.SubWatcher.Remove(_clientToServiceQueue);
        _subChannel?.TryBasicCancel(_consumerTag, _logger);
        _subChannel?.Close();
    }

    public void Start(bool isPost)
    {
        if (!isPost)
        {
            _conn.MainChannel.BasicReturn += BasicReturn;
            _firstCid = Guid.NewGuid().ToString("N");
            _subChannel = _conn.SubConnection.CreateModel();
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
        _conn.MainChannel.BasicPublish("", _conn.Options.RpcQueue, p, buffer);
        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(null));
    }

    private async Task SendFirstAsync(ReadOnlyMemory<byte> buffer, int mqPriority)
    {
        var mainQueueCount = _conn.GetMainQueueCount();
        if (mainQueueCount == -1)
        {
            var msg = $"mainQueueCount is -1, check queue if exist : {_conn.Options.RpcQueue}.";
            _logger.LogWarning(msg);
            throw new InvalidOperationException(msg);
        }
        
        if (_conn.Options.MaxQueueCount != 0 && mainQueueCount >= _conn.Options.MaxQueueCount)
            throw new MaxQueueCountInnerException(mainQueueCount);

        var p = CreateProp(mqPriority);
        p.ReplyTo = _serviceToClientQueue;
        p.CorrelationId = _firstCid;
        _conn.MainChannel.BasicPublish("", _conn.Options.RpcQueue, true, p, buffer);

        try
        {
            _clientToServiceQueue = await _clientToServiceQueueOnceBlock.WriteOnceBlock.ReceiveAsync(_conn.Options.FirstReplyTimeOut, _firstCts.Token);
        }
        catch (OperationCanceledException)
        {
            var msg = $"message has not sent to queue, check queue if exist : {_conn.Options.RpcQueue}.";
            _logger.LogWarning(msg);
            throw new InvalidOperationException(msg);
        }
        catch (TimeoutException)
        {
            var msg = $"wait first reply timeout, {_conn.Options.FirstReplyTimeOut.TotalSeconds} seconds.";
            _logger.LogWarning(msg);
            throw new MqHandshakeInnerException(mainQueueCount);
        }

        _conn.SubWatcher.Add(_clientToServiceQueue);
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
        var p = _conn.MainChannel.CreateBasicProperties();
        if (mqPriority != 0)
            p.Priority = (byte)mqPriority;
        return p;
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }
}