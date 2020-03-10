using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Base;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientConnection : IClientConnection
    {
        private readonly MQOptions _opt;
        private readonly RabbitMQOnceCall _call;

        public RabbitMQClientConnection(IConnection connect, MQOptions opt, ILogger logger)
        {
            _opt = opt;
            _call = new RabbitMQOnceCall(connect, opt.RpcQueue, logger);
            _call.Received += CallReceived;
        }

        private void CallReceived(object sender, global::RabbitMQ.Base.EventArgsT<byte[]> e)
        {
            if (e.Value == null)
                OnReceived(new EventArgsT<byte[]>(NullReply.All));
            else
                OnReceived(new EventArgsT<byte[]>(e.Value));
        }

        public void Dispose()
        {
            _call.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
#endif

        public ConnectionInfo ConnectionInfo => new ConnectionInfo
        {
            Host = _opt.Host,
            Port = _opt.Port,
            Description = _opt.ToString(),
            ChannelType = ChannelType.RabbitMQ
        };

        public event EventHandler<EventArgsT<byte[]>> Received;

        public event EventHandler<EventArgsT<Exception>> ReceiveDisconnected;

        public Task SendAsync(byte[] buffer, bool isEnd = false, bool isPost = false)
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