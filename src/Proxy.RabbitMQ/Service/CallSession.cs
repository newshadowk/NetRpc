using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class CallSession : IDisposable
    {
        private readonly BasicDeliverEventArgs _e;

        private readonly IModel _mainModel;
        private readonly IModel _clientToServiceModel;
        private readonly string _serviceToClientQueue;
        private string _clientToServiceQueue;

        public event EventHandler<EventArgsT<byte[]>> Received;

        public CallSession(IModel mainModel, IModel clientToServiceModel, BasicDeliverEventArgs e)
        {
            _mainModel = mainModel;
            _clientToServiceModel = clientToServiceModel;
            _serviceToClientQueue = e.BasicProperties.ReplyTo;
            _e = e;
        }

        public void Start()
        {
            _clientToServiceQueue = _clientToServiceModel.QueueDeclare().QueueName;

            var replyConsumer = new EventingBasicConsumer(_mainModel);
            replyConsumer.Received += (s, e) =>
            {
                OnReceived(new EventArgsT<byte[]>(e.Body));
            };

            _clientToServiceModel.BasicConsume(_clientToServiceQueue, true, replyConsumer);
            Send(Encoding.UTF8.GetBytes(_clientToServiceQueue));

            OnReceived(new EventArgsT<byte[]>(_e.Body));
        }

        public void Send(byte[] buffer)
        {
            _mainModel.BasicPublish("", _serviceToClientQueue, null, buffer);
        }

        public void Dispose()
        {
            _mainModel.BasicAck(_e.DeliveryTag, false);
            if (_clientToServiceQueue != null)
                _clientToServiceModel.QueueDelete(_clientToServiceQueue);
        }

        private void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}