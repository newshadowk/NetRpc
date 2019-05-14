using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Base;

namespace NetRpc.Grpc
{
    public sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly object[] _instances;
        private readonly MiddlewareRegister _middlewareRegister = new MiddlewareRegister();

        public MessageCallImpl(object[] instances)
        {
            _instances = instances;
        }

        public void UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : MiddlewareBase
        {
            _middlewareRegister.UseMiddleware<TMiddleware>(args);
        }

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            using (var serviceOnceTransfer = new ServiceConnection(requestStream, responseStream))
            {
                await NetRpc.HandleRequestAsync(serviceOnceTransfer, _instances, _middlewareRegister);
            }
        }
    }
}