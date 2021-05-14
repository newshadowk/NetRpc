using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientOnceApiConvert : IAsyncDisposable
    {
        ConnectionInfo ConnectionInfo { get; }

        Task StartAsync(string? authorizationToken);

        Task SendCancelAsync();

        Task SendBufferAsync(ReadOnlyMemory<byte> body);

        Task SendBufferEndAsync();

        /// <returns>True do not send stream next, otherwise false.</returns>
        Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream? stream, bool isPost, byte mqPriority, CancellationToken token);

        event EventHandler<EventArgsT<object>>? ResultStream;
        event AsyncEventHandler<EventArgsT<object?>>? ResultAsync;
        event AsyncEventHandler<EventArgsT<object>>? CallbackAsync;
        event AsyncEventHandler<EventArgsT<object>>? FaultAsync;
    }
}