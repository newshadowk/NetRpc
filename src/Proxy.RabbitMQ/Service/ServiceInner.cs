using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class ServiceInner : IDisposable
    {
        public event EventHandler<EventArgsT<CallSession>> Received;
        private readonly string _rpcQueueName;
        private readonly int _prefetchCount;
        private readonly ILogger _logger;
        private readonly IConnection _connect;
        private volatile IModel _mainModel;

        public ServiceInner(IConnection connect, string rpcQueueName, int prefetchCount, ILogger logger)
        {
            _connect = connect;
            _rpcQueueName = rpcQueueName;
            _prefetchCount = prefetchCount;
            _logger = logger;
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
            OnReceived(new EventArgsT<CallSession>(new CallSession(_connect, _mainModel, e, _logger)));
        }

        public void Dispose()
        {
            try
            {
                _mainModel?.Close();
                _mainModel?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        private void OnReceived(EventArgsT<CallSession> e)
        {
            Received?.Invoke(this, e);
        }
    }
}