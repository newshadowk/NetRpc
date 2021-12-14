using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using Proxy.Grpc;

namespace NetRpc.Grpc;

public sealed class MessageCallImpl : MessageCall.MessageCallBase
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

        await using var connection = new GrpcServiceConnection(requestStream, responseStream, _logger, context.CancellationToken);
        try
        {
            await _requestHandler.HandleAsync(connection, ChannelType.Grpc);
        }
        finally
        {
            _busyFlag.Decrement();
        }
    }
}