using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IServiceAsync
    {
        Task Call(string s);

        Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token);
    }

    public interface IService2Async
    {
        Task Call2(string s);
    }
}