using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class CallSession : IDisposable
    {
        private readonly IModel _mainModel;
        private readonly BasicDeliverEventArgs _e;

        private readonly IModel _clientToServiceModel;
        private readonly string _serviceToClientQueue;
        private string _clientToServiceQueue;
        private bool _disposed;
        private readonly bool _isPost;

        public event EventHandler<EventArgsT<byte[]>> Received;

        public CallSession(IConnection connection, IModel mainModel, BasicDeliverEventArgs e)
        {
            _clientToServiceModel = connection.CreateModel();
            _isPost = e.BasicProperties.ReplyTo == null;
            _serviceToClientQueue = e.BasicProperties.ReplyTo;
            _mainModel = mainModel;
            _e = e;
        }

        public void Start()
        {
            if (!_isPost)
            {
                _clientToServiceQueue = _clientToServiceModel.QueueDeclare().QueueName;
                var replyConsumer = new EventingBasicConsumer(_clientToServiceModel);
                replyConsumer.Received += (s, e) => { OnReceived(new EventArgsT<byte[]>(e.Body)); };

                _clientToServiceModel.BasicConsume(_clientToServiceQueue, true, replyConsumer);
                Send(Encoding.UTF8.GetBytes(_clientToServiceQueue));
            }

            OnReceived(new EventArgsT<byte[]>(_e.Body));
        }

        public void Send(byte[] buffer)
        {
            _clientToServiceModel.BasicPublish("", _serviceToClientQueue, null, buffer);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _mainModel.BasicAck(_e.DeliveryTag, false);
            if (_clientToServiceQueue != null)
            {
                _clientToServiceModel.Close();
                _clientToServiceModel.Dispose();
            }
        }

        private void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}