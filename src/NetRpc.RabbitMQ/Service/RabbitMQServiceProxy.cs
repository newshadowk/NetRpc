using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public sealed class RabbitMQServiceProxy : IHostedService
    {
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> RecoverySucceeded;
        private RequestHandler _requestHandler;
        private Service _service;

        public RabbitMQServiceProxy(IOptionsMonitor<RabbitMQServiceOptions> mqOptions, IServiceProvider serviceProvider)
        {
            Reset(mqOptions.CurrentValue.Value, serviceProvider);
            mqOptions.OnChange(i =>
            {
                Reset(i.Value, serviceProvider);
                _service.Open();
            });
        }

        private void Reset(MQOptions opt, IServiceProvider serviceProvider)
        {
            if (_service != null)
            {
                _service.Dispose();
                _service.Received -= ServiceReceived;
                _service.RecoverySucceeded -= ConnectRecoverySucceeded;
                _service.ConnectionShutdown -= ConnectConnectionShutdown;
            }

            _service = new Service(opt.CreateConnectionFactory(), opt.RpcQueue, opt.PrefetchCount);
            _requestHandler = new RequestHandler(serviceProvider);
            _service.Received += ServiceReceived;
            _service.RecoverySucceeded += ConnectRecoverySucceeded;
            _service.ConnectionShutdown += ConnectConnectionShutdown;
        }

        private async void ServiceReceived(object sender, global::RabbitMQ.Base.EventArgsT<CallSession> e)
        {
            using (var connection = new RabbitMQServiceConnection(e.Value))
                await _requestHandler.HandleAsync(connection);
        }

        private void ConnectConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            OnConnectionShutdown(e);
        }

        private void ConnectRecoverySucceeded(object sender, EventArgs e)
        {
            OnRecoverySucceeded();
        }

        private void OnConnectionShutdown(ShutdownEventArgs e)
        {
            ConnectionShutdown?.Invoke(this, e);
        }

        private void OnRecoverySucceeded()
        {
            RecoverySucceeded?.Invoke(this, EventArgs.Empty);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.Open();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _service?.Dispose();
            return Task.CompletedTask;
        }
    }
}