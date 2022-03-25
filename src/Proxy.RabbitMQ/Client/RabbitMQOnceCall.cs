using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class RabbitMQOnceCall : IDisposable
    {
        private readonly CheckWriteOnceBlock<string> _clientToServiceQueueOnceBlock = new();
        private readonly IConnection _connect;
        private readonly ILogger _logger;
        private readonly string _rpcQueue;
        private string _clientToServiceQueue = null!;
        private volatile bool _disposed;
        private volatile IModel? _channel;
        private readonly int _maxPriority;
        private string _serviceToClientQueue = null!;
        private bool isFirstSend = true;
        private volatile string? _consumerTag;

        public RabbitMQOnceCall(IConnection connect, string rpcQueue, int maxPriority, ILogger logger)
        {
            _connect = connect;
            _rpcQueue = rpcQueue;
            _maxPriority = maxPriority;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                if (_channel != null)
                {
                    if (_consumerTag != null)
                        _channel.BasicCancel(_consumerTag);
                    _channel.Close();
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>?>>? ReceivedAsync;

        public async Task CreateChannelAsync()
        {
            //bug:block issue, need start a task. a thread context per channel.
            await Task.Run(() =>
            {
                _channel = _connect.CreateModel();
                _channel.QueueDeclare(_rpcQueue, false, false, false,
                    (_maxPriority > 0 ? new Dictionary<string, object> { { "x-max-priority", _maxPriority } } : null)!);
            });
            _serviceToClientQueue = _channel!.QueueDeclare().QueueName;
            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.Received += ConsumerReceivedAsync;
            _consumerTag = _channel!.BasicConsume(_serviceToClientQueue, true, consumer);
        }

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isPost, int mqPriority = 0)
        {
            if (isPost)
            {
                var p = CreateProp(mqPriority);
                _channel!.BasicPublish("", _rpcQueue, p, buffer);
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

                _channel!.BasicReturn += (_, args) =>
                {
                    if (args.BasicProperties.CorrelationId == cid)
                        cts.Cancel();
                };

                _channel.BasicPublish("", _rpcQueue, true, p, buffer);

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
                _channel!.BasicPublish("", _clientToServiceQueue, null!, buffer);

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
            var p = _channel!.CreateBasicProperties();
            if (mqPriority != 0)
                p.Priority = (byte)mqPriority;
            return p;
        }
    }
}