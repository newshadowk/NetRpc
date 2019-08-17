using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Base;

namespace NetRpc.Grpc
{
    internal sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly RequestHandler _requestHandler;

        public MessageCallImpl(IServiceProvider serviceProvider)
        {
            _requestHandler = new RequestHandler(serviceProvider);
        }

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            using (var connection = new GrpcServiceConnection(requestStream, responseStream))
                await _requestHandler.HandleAsync(connection);
        }
    }
}