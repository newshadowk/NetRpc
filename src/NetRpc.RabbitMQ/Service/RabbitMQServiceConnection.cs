using System;
using System.Threading.Tasks;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    internal class RabbitMQServiceConnection : IServiceConnection
    {
        private readonly CallSession _callSession;

        public RabbitMQServiceConnection(CallSession callSession)
        {
            _callSession = callSession;
            _callSession.Received += CallSessionReceived;
        }

        private void CallSessionReceived(object sender, global::RabbitMQ.Base.EventArgsT<byte[]> e)
        {
            OnReceived(new EventArgsT<byte[]>(e.Value));
        }

        public void Dispose()
        {
            _callSession.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
#endif

        public event EventHandler<EventArgsT<byte[]>> Received;

        public Task SendAsync(byte[] buffer)
        {
            return Task.Run(() => { _callSession.Send(buffer); });
        }

        public Task StartAsync()
        {
            _callSession.Start();
            return Task.CompletedTask;
        }

        protected virtual void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}