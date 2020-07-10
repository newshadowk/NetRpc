using System;
using System.Threading.Tasks;

namespace NetRpc.Grpc
{
    internal sealed class NamedPipeClientConnection : IClientConnection
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ConnectionInfo ConnectionInfo { get; }
        public event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;
        public event EventHandler<EventArgsT<Exception>> ReceiveDisconnected;
        public Task SendAsync(byte[] buffer, bool isEnd = false, bool isPost = false)
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(string authorizationToken)
        {
            throw new NotImplementedException();
        }
    }
}