using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class ServiceInner : IDisposable
    {
        public event EventHandler<EventArgsT<CallSession>> Received;
        private readonly string _rpcQueueName;
        private readonly int _prefetchCount;
        private readonly IConnection _connect;
        private volatile IModel _mainModel;

        public ServiceInner(IConnection connect, string rpcQueueName, int prefetchCount)
        {
            _connect = connect;
            _rpcQueueName = rpcQueueName;
            _prefetchCount = prefetchCount;
        }

        public void CreateChannel()
        {
            _mainModel = _connect.CreateModel();
            _mainModel.QueueDeclare(_rpcQueueName, false, false, true, null);
            var consumer = new EventingBasicConsumer(_mainModel);
            _mainModel.BasicQos(0, (ushort) _prefetchCount, true);
            _mainModel.BasicConsume(_rpcQueueName, false, consumer);
            consumer.Received += ConsumerReceived;
        }

        private void ConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            OnReceived(new EventArgsT<CallSession>(new CallSession(_connect, _mainModel, e)));
        }

        public void Dispose()
        {
            _mainModel?.Close();
            _mainModel?.Dispose();
        }

        private void OnReceived(EventArgsT<CallSession> e)
        {
            Received?.Invoke(this, e);
        }
    }
}