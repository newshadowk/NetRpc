using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class ServiceInner : IDisposable
    {
        public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
        private readonly string _rpcQueueName;
        private readonly int _prefetchCount;
        private readonly ILogger _logger;
        private readonly IConnection _connect;
        private volatile IModel? _mainModel;
        private volatile bool _disposed;

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
            var consumer = new AsyncEventingBasicConsumer(_mainModel);
            _mainModel.BasicQos(0, (ushort) _prefetchCount, true);
            _mainModel.BasicConsume(_rpcQueueName, false, consumer);
            consumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_connect, _mainModel, e, _logger)));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

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

        private Task OnReceivedAsync(EventArgsT<CallSession> e)
        {
            return ReceivedAsync.InvokeAsync(this, e);
        }
    }
}