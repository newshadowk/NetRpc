using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IBufferServiceOnceApiConvert : IServiceOnceApiConvert
    {
        Task SendBufferAsync(byte[] buffer);

        Task SendBufferEndAsync();

        Task SendBufferCancelAsync();

        Task SendBufferFaultAsync();
    }

    public interface IHttpServiceOnceApiConvert : IServiceOnceApiConvert
    {
        Task SendStreamAsync(Stream stream, string streamName);
    }

    public interface IServiceOnceApiConvert : IDisposable
    {
        void Start(CancellationTokenSource cts);

        Task<OnceCallParam> GetOnceCallParamAsync();

        Task SendResultAsync(CustomResult result, RpcContext context);

        Task SendFaultAsync(Exception body, RpcContext context);

        Task SendCallbackAsync(object callbackObj);

        Stream GetRequestStream(long? length);
    }
}