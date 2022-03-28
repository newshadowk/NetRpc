using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base;

public sealed class RabbitMQOnceCall : IDisposable
{
    private readonly CheckWriteOnceBlock<string> _clientToServiceQueueOnceBlock = new();
    private readonly IModel _cmdChannel;
    private readonly IModel _tmpChannel;
    private readonly ILogger _logger;
    private readonly string _rpcQueue;
    private string _clientToServiceQueue = null!;
    private volatile bool _disposed;
    private string _serviceToClientQueue = null!;
    private bool isFirstSend = true;
    private volatile string? _consumerTag;

    public RabbitMQOnceCall(IModel cmdChannel, IModel tmpChannel, string rpcQueue, ILogger logger)
    {
        _cmdChannel = cmdChannel;
        _tmpChannel = tmpChannel;
        _rpcQueue = rpcQueue;
        _logger = logger;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            if (_consumerTag != null && !_tmpChannel.IsClosed)
                _tmpChannel.BasicCancel(_consumerTag);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
        }
    }

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>?>>? ReceivedAsync;

    public void CreateChannel()
    {
        _serviceToClientQueue = _tmpChannel.QueueDeclare().QueueName;
        var consumer = new AsyncEventingBasicConsumer(_tmpChannel);
        consumer.Received += ConsumerReceivedAsync;
        _consumerTag = _tmpChannel.BasicConsume(_serviceToClientQueue, true, consumer);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isPost, int mqPriority = 0)
    {
        if (isPost)
        {
            var p = CreateProp(mqPriority);
            _cmdChannel.BasicPublish("", _rpcQueue, p, buffer);
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(null));
            return;
        }

        if (isFirstSend)
        {
            var p = CreateProp(mqPriority);
            p.ReplyTo = _serviceToClientQueue;
            var cid = Guid.NewGuid().ToString();
            p.CorrelationId = cid;

            var cts = new CancellationTokenSource();

            _cmdChannel.BasicReturn += (_, args) =>
            {
                _logger.LogInformation($"Cmd send to queue failed, BasicReturn, ReplyCode:{args.ReplyCode} Exchange:{args.Exchange}, RoutingKey:{args.RoutingKey}, ReplyText:{args.ReplyText}");
                if (args.BasicProperties.CorrelationId == cid)
                    cts.Cancel();
            };

            _cmdChannel.BasicPublish("", _rpcQueue, true, p, buffer);

            try
            {
                _clientToServiceQueue = await _clientToServiceQueueOnceBlock.WriteOnceBlock.ReceiveAsync(cts.Token);
                isFirstSend = false;
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException($"Message has not sent to queue, check queue if exist : {_rpcQueue}.");
            }
        }
        else
            _cmdChannel.BasicPublish("", _clientToServiceQueue, null!, buffer);

        //bug: after invoke 'BasicPublish' need an other thread to publish for real send? (sometimes happened.)
        //blocking thread in OnceCall row 96:Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
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
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(e.Body));
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>?> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }

    private IBasicProperties CreateProp(int mqPriority)
    {
        var p = _tmpChannel!.CreateBasicProperties();
        if (mqPriority != 0)
            p.Priority = (byte)mqPriority;
        return p;
    }
}