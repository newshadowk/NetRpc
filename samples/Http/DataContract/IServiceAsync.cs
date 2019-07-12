using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IServiceAsync
    {
        //Task<CustomObj> Call(string p1, int p2, Guid p3, DateTime p4);

        Task<Stream> EchoStreamAsync(string p1, Stream data);

        //Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}