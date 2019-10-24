using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientOnceApiConvert : IDisposable
    {
        Task StartAsync();

        Task SendCancelAsync();

        Task SendBufferAsync(byte[] body);

        Task SendBufferEndAsync();

        /// <returns>True do not send stream next, otherwise false.</returns>
        Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream stream, bool isPost, CancellationToken token);

        event EventHandler<EventArgsT<object>> ResultStream;
        event EventHandler<EventArgsT<object>> Result;
        event EventHandler<EventArgsT<object>> Callback;
        event EventHandler<EventArgsT<object>> Fault;
    }
}