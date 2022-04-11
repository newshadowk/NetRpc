using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    //[ClientRetry(100)]
    Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token);

    Task<string> Call2(string s);

    [MQPost]
    Task P(CustomObj obj);
}