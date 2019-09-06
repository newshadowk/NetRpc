using System;
using System.Threading.Tasks;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientConnection : IClientConnection
    {
        private readonly RabbitMQOnceCall _call;

        public RabbitMQClientConnection(global::RabbitMQ.Client.IConnection connect, string rpcQueue)
        {
            _call = new RabbitMQOnceCall(connect, rpcQueue);
            _call.Received += CallReceived;
        }

        private void CallReceived(object sender, global::RabbitMQ.Base.EventArgsT<byte[]> e)
        {
            OnReceived(new EventArgsT<byte[]>(e.Value));
        }

        public void Dispose()
        {
            _call.Dispose();
        }

        public event EventHandler<EventArgsT<byte[]>> Received;

        public Task SendAsync(byte[] buffer, bool isPost)
        {
            return _call.Send(buffer, isPost);
        }

        public Task StartAsync()
        {
            _call.CreateChannel();
            return Task.CompletedTask;
        }

        protected virtual void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}