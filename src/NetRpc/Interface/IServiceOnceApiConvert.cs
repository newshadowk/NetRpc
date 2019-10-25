using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IServiceOnceApiConvert : IDisposable
    {
        Task SendBufferAsync(byte[] buffer);

        Task SendBufferEndAsync();

        Task SendBufferCancelAsync();

        Task SendBufferFaultAsync();

        Task StartAsync(CancellationTokenSource cts);

        Task<OnceCallParam> GetOnceCallParamAsync();

        /// <returns>True need send stream next, otherwise false.</returns>
        Task<bool> SendResultAsync(CustomResult result, Stream stream, string streamName, ActionExecutingContext context);

        Task SendFaultAsync(Exception body, ActionExecutingContext context);

        Task SendCallbackAsync(object callbackObj);

        Stream GetRequestStream(long? length);
    }
}