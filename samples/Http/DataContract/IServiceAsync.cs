using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Http.FaultContract;

namespace DataContract
{
    public interface IServiceAsync
    {
        //Task<CustomObj> Call(string p1, int p2);

        //[SwaggerFaultContract(typeof(CustomException))]
        //[SwaggerFaultContract(typeof(CustomException2))]
        //Task CallByCustomExceptionAsync();

        //Task<Stream> EchoStreamAsync(Stream stream);

        //Task<ComplexStream> GetComplexStreamAsync();

        Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}