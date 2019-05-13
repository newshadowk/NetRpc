using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

        public async Task Send(byte[] buffer)
        {
            if (isFirstSend)
            {
                var p = _model.CreateBasicProperties();
                p.ReplyTo = _serviceToClientQueue;
                _model.BasicPublish("", _rpcQueue, p, buffer);
                _clientToServiceQueue = await _clientToServiceQueueOnceBlock.WriteOnceBlock.ReceiveAsync();
                isFirstSend = false;
            }
            else
            {
                _model.BasicPublish("", _clientToServiceQueue, null, buffer);
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