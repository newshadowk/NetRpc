using System;
using RabbitMQ.Client;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public sealed class ServiceProxy : IDisposable
    {
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> RecoverySucceeded;

        private readonly Service _service;

        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister = new MiddlewareRegister();

        public ServiceProxy(Service service, object[] instances)
        {
            _service = service;
            _instances = instances;
            _service.Received += ServiceReceived;
            _service.RecoverySucceeded += ConnectRecoverySucceeded;
            _service.ConnectionShutdown += ConnectConnectionShutdown;
        }

        private async void ServiceReceived(object sender, global::RabbitMQ.Base.EventArgsT<CallSession> e)
        {
            using (var serviceOnceTransfer = new ServiceConnection(e.Value))
                await NetRpc.HandleRequestAsync(serviceOnceTransfer, _instances, _middlewareRegister);
        }

        public void UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : MiddlewareBase
        {
            _middlewareRegister.UseMiddleware<TMiddleware>(args);
        }

        public void Open()
        {
            _service.Open();
        }

        public void Dispose()
        {
            _service?.Dispose();
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
    }
}