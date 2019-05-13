using System;
using System.Threading.Tasks;
using RabbitMQ.Base;

namespace Nrpc.RabbitMQ
{
    internal class ServiceConnection : IConnection
    {
        private readonly CallSession _callSession;

        public ServiceConnection(CallSession callSession)
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

        public event EventHandler<EventArgsT<byte[]>> Received;

        public Task Send(byte[] buffer)
        {
            return Task.Run(() =>
            {
                _callSession.Send(buffer);
            });
        }

        public void Start()
        {
            _callSession.Start();
        }

        protected virtual void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}