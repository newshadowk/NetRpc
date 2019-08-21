using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Base;

namespace NetRpc.Grpc
{
    internal sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly RequestHandler _requestHandler;
        private volatile int _handlingCount;

        public MessageCallImpl(IServiceProvider serviceProvider)
        {
            _requestHandler = new RequestHandler(serviceProvider);
        }

        public bool IsHanding => _handlingCount > 0;

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            Interlocked.Increment(ref _handlingCount);
            try
            {
                using (var connection = new GrpcServiceConnection(requestStream, responseStream))
                    await _requestHandler.HandleAsync(connection);
            }
            finally
            {
                Interlocked.Decrement(ref _handlingCount);
            }
        }
    }
}