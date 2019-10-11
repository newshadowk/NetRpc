using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IServiceAsync
    {
        Task<CustomObj> SetAndGetObj(CustomObj obj);

        /// <exception cref="TaskCanceledException"></exception>
        Task CallByCancelAsync(CancellationToken token);

        Task CallByCallBackAsync(Action<CustomCallbackObj> cb);

        /// <exception cref="NotImplementedException"></exception>
        Task CallBySystemExceptionAsync();

        /// <exception cref="CustomException"></exception>>
        Task CallByCustomExceptionAsync();

        Task<Stream> GetStreamAsync();

        Task SetStreamAsync(Stream data);

        Task<Stream> EchoStreamAsync(Stream data);

        Task<ComplexStream> GetComplexStreamAsync();

        /// <exception cref="TaskCanceledException"></exception>
        Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}