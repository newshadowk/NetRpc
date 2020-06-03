using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    public interface IClientOnceApiConvert : IDisposable, IAsyncDisposable
#else
    public interface IClientOnceApiConvert : IDisposable
#endif
    {
        ConnectionInfo ConnectionInfo { get; }

        Task StartAsync(string authorizationToken);

        Task SendCancelAsync();

        Task SendBufferAsync(byte[] body);

        Task SendBufferEndAsync();

        /// <returns>True do not send stream next, otherwise false.</returns>
        Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream stream, bool isPost, CancellationToken token);

        event EventHandler<EventArgsT<object>> ResultStream;
        event EventHandler<EventArgsT<object>> Result;

        event Func<object, EventArgsT<object>, Task> CallbackAsync;
        event EventHandler<EventArgsT<object>> Fault;
    }
}