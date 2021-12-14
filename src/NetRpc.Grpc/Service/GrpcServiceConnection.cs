using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proxy.Grpc;

namespace NetRpc.Grpc;

internal sealed class GrpcServiceConnection : IServiceConnection
{
    private readonly AsyncLock _sendLock = new();
    private readonly IAsyncStreamReader<StreamBuffer> _requestStream;
    private readonly IServerStreamWriter<StreamBuffer> _responseStream;
    private readonly ILogger _logger;
    private readonly WriteOnceBlock<bool> _end = new(i => i);

    public GrpcServiceConnection(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream, ILogger logger)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        //before dispose requestStream need to
        //wait 60 second to receive 'completed' from client side.
        await Task.WhenAny(Task.Delay(1000 * 60),
            _end.ReceiveAsync());
    }

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;
    public event AsyncEventHandler? DisconnectedAsync;

    public async Task SendAsync(ReadOnlyMemory<byte> buffer)
    {
        //add a lock here will not slowdown send speed.
        using (await _sendLock.LockAsync())
        {
            try
            {
                await _responseStream.WriteAsync(new StreamBuffer {Body = ByteString.CopyFrom(buffer.Span)});
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Service WriteAsync error. ");
                await OnDisconnectedAsync(EventArgs.Empty);
                throw;
            }
        }
    }

    public Task StartAsync()
    {
        Task.Run(async () =>
        {
            //MoveNext will have a Exception when client is disconnected.
            try
            {
                while (await _requestStream.MoveNext(CancellationToken.None))
                    await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(_requestStream.Current.Body.ToByteArray()));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Service MoveNext error. ");
                await OnDisconnectedAsync(EventArgs.Empty);
            }
            finally
            {
                _end.Post(true);
            }
        });

        return Task.CompletedTask;
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }

    private Task OnDisconnectedAsync(EventArgs e)
    {
        return DisconnectedAsync.InvokeAsync(this, e);
    }
}