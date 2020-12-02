using System;
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
        private volatile IModel? _model;
        private string _serviceToClientQueue = null!;
        private bool isFirstSend = true;

        public RabbitMQOnceCall(IConnection connect, string rpcQueue, ILogger logger)
        {
            _connect = connect;
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
                _model?.Close();
                _model?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>?>>? ReceivedAsync;

        public async Task CreateChannelAsync()
        {
            //bug:block issue, need start a task.
            await Task.Run(() => { _model = _connect.CreateModel(); });

            _serviceToClientQueue = _model.QueueDeclare().QueueName;
            var consumer = new AsyncEventingBasicConsumer(_model);
            _model.BasicConsume(_serviceToClientQueue, true, consumer);
            consumer.Received += ConsumerReceived;
        }

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isPost)
        {
            if (isPost)
            {
                var p = _model!.CreateBasicProperties();
                _model.BasicPublish("", _rpcQueue, p, buffer);
                await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>?>(null));
                return;
            }

            if (isFirstSend)
            {
                var p = _model!.CreateBasicProperties();
                p.ReplyTo = _serviceToClientQueue;
                var cid = Guid.NewGuid().ToString();
                p.CorrelationId = cid;

                var cts = new CancellationTokenSource();

                _model.BasicReturn += (sender, args) =>
                {
                    if (args.BasicProperties.CorrelationId == cid)
                        cts.Cancel();
                };

                _model.BasicPublish("", _rpcQueue, true, p, buffer);

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
                _model.BasicPublish("", _clientToServiceQueue, null, buffer);

            //bug: after invoke 'BasicPublish' need an other thread to publish for real send? (sometimes happened.)
            //blocking thread in OnceCall row 96:Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
        }

        private async Task ConsumerReceived(object s, BasicDeliverEventArgs e)
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
    }
}