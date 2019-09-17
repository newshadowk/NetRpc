using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NetRpc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class RabbitMQOnceCall : IDisposable
    {
        private volatile IModel _model;
        private readonly IConnection _connect;
        private readonly string _rpcQueue;
        private string _serviceToClientQueue;
        private string _clientToServiceQueue;
        private readonly CheckWriteOnceBlock<string> _clientToServiceQueueOnceBlock = new CheckWriteOnceBlock<string>();
        private bool isFirstSend = true;
        public event EventHandler<EventArgsT<byte[]>> Received;

        public RabbitMQOnceCall(IConnection connect, string rpcQueue)
        {
            _connect = connect;
            _rpcQueue = rpcQueue;
        }

        public void CreateChannel()
        {
            _model = _connect.CreateModel();
            _serviceToClientQueue = _model.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(_model);
            _model.BasicConsume(_serviceToClientQueue, true, consumer);
            consumer.Received += ConsumerReceived;
        }

        public async Task Send(byte[] buffer, bool isPost)
        {
            if (isPost)
            {
                var p = _model.CreateBasicProperties();
                _model.BasicPublish("", _rpcQueue, p, buffer);
                OnReceived(new EventArgsT<byte[]>(NullReply.All));
                return;
            }

            if (isFirstSend)
            {
                IBasicProperties p = _model.CreateBasicProperties();
                p.ReplyTo = _serviceToClientQueue;
                var cid = Guid.NewGuid().ToString();
                p.CorrelationId = cid;

                CancellationTokenSource cts = new CancellationTokenSource();

                _model.BasicReturn += (sender, args) =>
                {
                    if (args.BasicProperties.CorrelationId == cid)
                        cts.Cancel();
                };

                _model.BasicPublish("", _rpcQueue, true, p, buffer);

                try
                {
                    _clientToServiceQueue = await _clientToServiceQueueOnceBlock.WriteOnceBlock.ReceiveAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new MessageDeliverException($"Message has not sent to queue, check queue if exist : {_rpcQueue}.");
                }

                isFirstSend = false;
            }
            else
            {
                _model.BasicPublish("", _clientToServiceQueue, null, buffer);
                //bug: after invoke 'BasicPublish' need an other thread to publish for real send? (sometimes happened.)
                //blocking thread in OnceCall row 96:Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
            }
        }
    
        private void ConsumerReceived(object s, BasicDeliverEventArgs e)
        {
            if (!_clientToServiceQueueOnceBlock.IsPosted)
            {
                lock (_clientToServiceQueueOnceBlock.SyncRoot)
                {
                    if (!_clientToServiceQueueOnceBlock.IsPosted)
                    {
                        _clientToServiceQueueOnceBlock.IsPosted = true;
                        _clientToServiceQueueOnceBlock.WriteOnceBlock.Post(Encoding.UTF8.GetString(e.Body));
                    }
                }
            }
            else
            {
                OnReceived(new EventArgsT<byte[]>(e.Body));
            }
        }

        public void Dispose()
        {
            _model?.Close();
            _model?.Dispose();
        }

        private void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}