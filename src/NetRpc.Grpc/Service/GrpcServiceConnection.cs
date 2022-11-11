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
    private readonly CancellationTokenRegistration _grpcTokenReg;

    public GrpcServiceConnection(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream, ILogger logger,
        CancellationToken grpcToken)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
        _logger = logger;
        _grpcTokenReg = grpcToken.Register(() => OnDisconnectedAsync(EventArgs.Empty));
    }

    public async ValueTask DisposeAsync()
    {
        await _grpcTokenReg.DisposeAsync();

        //before dispose requestStream need to
        //wait 60 second to receive 'completed' from client side.

        // ReSharper disable MethodSupportsCancellation
        await Task.WhenAny(Task.Delay(1000 * 60), _end.ReceiveAsync());
        // ReSharper restore MethodSupportsCancellation
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
                DebugI($"Send buffer len:{buffer.Span.Length}");
                await _responseStream.WriteAsync(new StreamBuffer { Body = ByteString.CopyFrom(buffer.Span) });
                DebugI("Send buffer end.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(GlobalDebugContext.Context.ToString());
                _logger.LogWarning(e, "Service WriteAsync error. ");
                await OnDisconnectedAsync(EventArgs.Empty);
                throw;
            }
        }
    }

    private static void DebugI(string s)
    {
        GlobalDebugContext.Context.Info(s);
    }

    public Task<bool> StartAsync()
    {
        DebugI("start.");

        Task.Run(async () =>
        {
            //MoveNext will have a Exception when client is disconnected.
            try
            {
                DebugI("MoveNext, start");
                while (await _requestStream.MoveNext(CancellationToken.None))
                {
                    DebugI("MoveNext, in");
                    try
                    {
                        var buffer = _requestStream.Current.Body.ToByteArray();
                        DebugI($"MoveNext, buffer len:{buffer.Length}");
                        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(buffer));
                        DebugI("MoveNext, end.");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Service OnReceivedAsync error. ");
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(GlobalDebugContext.Context.ToString());
                _logger.LogWarning(e, "Service MoveNext error. ");
                await OnDisconnectedAsync(EventArgs.Empty);
            }
            finally
            {
                _end.Post(true);
            }
        });

        return Task.FromResult(true);
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