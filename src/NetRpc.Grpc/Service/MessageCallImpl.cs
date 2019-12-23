using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    internal sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly RequestHandler _requestHandler;
        private int _handlingCount;

        public MessageCallImpl(IServiceProvider serviceProvider)
        {
            _requestHandler = new RequestHandler(serviceProvider, ChannelType.Grpc);
        }

        public bool IsHanding => _handlingCount > 0;

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            Interlocked.Increment(ref _handlingCount);
            GrpcServiceConnection connection = null;
            try
            {
                connection = new GrpcServiceConnection(requestStream, responseStream);
                await _requestHandler.HandleAsync(connection);
            }
            finally
            {
                if (connection != null) 
                    await connection.AllDisposeAsync();
                Interlocked.Decrement(ref _handlingCount);
            }
        }
    }
}