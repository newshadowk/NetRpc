using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    internal sealed class MessageCallImpl : MessageCall.MessageCallBase
    {
        private readonly BusyFlag _busyFlag;
        private readonly RequestHandler _requestHandler;
        private readonly ILogger _logger;

        public MessageCallImpl(RequestHandler requestHandler, ILoggerFactory factory, BusyFlag busyFlag)
        {
            _busyFlag = busyFlag;
            _requestHandler = requestHandler;
            _logger = factory.CreateLogger("NetRpc");
        }

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            _busyFlag.Increment();
            GrpcServiceConnection connection = null;
            try
            {
                connection = new GrpcServiceConnection(requestStream, responseStream, _logger);
                await _requestHandler.HandleAsync(connection, ChannelType.Grpc);
            }
            finally
            {
                if (connection != null) 
                    await connection.AllDisposeAsync();
                _busyFlag.Decrement();
            }
        }
    }
}