using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    internal sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly RequestHandler _requestHandler;
        private int _handlingCount;
        private readonly ILogger _logger;

        public MessageCallImpl(IServiceProvider serviceProvider, ILogger logger)
        {
            _requestHandler = new RequestHandler(serviceProvider, ChannelType.Grpc);
            _logger = logger;
        }

        public bool IsHanding => _handlingCount > 0;

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            Interlocked.Increment(ref _handlingCount);
            GrpcServiceConnection connection = null;
            try
            {
                connection = new GrpcServiceConnection(requestStream, responseStream, _logger);
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