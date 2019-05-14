using System;
using System.Threading.Tasks;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ
{
    public class ClientConnection : IConnection
    {
        private readonly RabbitMQOnceCall _call;

        public ClientConnection(global::RabbitMQ.Client.IConnection connect, string rpcQueue)
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

        public Task Send(byte[] buffer)
        {
            return _call.Send(buffer);
        }

        public void Start()
        {
            _call.CreateChannel();
        }

        protected virtual void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}