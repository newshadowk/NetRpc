using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Http.FaultContract;

namespace DataContract
{
    public interface IServiceAsync
    {
        /// <summary>
        /// IServiceAsync Call
        /// </summary>
        /// <returns></returns>
        Task<CustomObj> Call(string p1, int p2);

        [SwaggerFaultContract(typeof(CustomException))]
        [SwaggerFaultContract(typeof(CustomException2))]
        Task CallByCustomExceptionAsync();

        Task<Stream> EchoStreamAsync(Stream stream);

        Task<ComplexStream> GetComplexStreamAsync();

        /// <summary>
        /// IServiceAsync ComplexCallAsync
        /// </summary>
        /// <returns></returns>
        Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}