using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class Service : IDisposable
    {
        public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
        private IConnection? _connection;
        private readonly ConnectionFactory _factory;
        private readonly string _rpcQueue;
        private readonly int _prefetchCount;
        private readonly int _maxPriority;
        private readonly ILogger _logger;
        private volatile bool _disposed;

        public ServiceInner? Inner { get; private set; }

        public Service(ConnectionFactory factory, string rpcQueue, int prefetchCount, int maxPriority, ILogger logger)
        {
            _factory = factory;
            _rpcQueue = rpcQueue;
            _prefetchCount = prefetchCount;
            _logger = logger;
            _maxPriority = maxPriority;
        }

        public void Open()
        {
            _connection = _factory.CreateConnectionLoop(_logger);
            Inner = new ServiceInner(_connection, _rpcQueue, _prefetchCount, _maxPriority, _logger);
            Inner.CreateChannel();
            Inner.ReceivedAsync += (_, e) => OnReceivedAsync(e);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                _connection?.Close();
                _connection?.Dispose();
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