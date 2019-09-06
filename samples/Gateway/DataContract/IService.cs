using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(string s);

        Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Action<CustomCallbackObj> cb, CancellationToken token);
    }

    public interface IService2
    {
        Task Call2(string s);
    }
}