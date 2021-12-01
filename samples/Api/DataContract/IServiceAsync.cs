using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    Task<CustomObj> SetAndGetObj(CustomObj obj);

    Task CallByCancelAsync(CancellationToken token);

    Task CallByCallBackAsync(Func<CustomCallbackObj, Task> cb);

    [FaultException(typeof(NotImplementedException))]  //defined only for http channel
    Task CallBySystemExceptionAsync();

    [FaultException(typeof(CustomException))]      //defined only for http channel
    Task CallByCustomExceptionAsync();

    Task<Stream> GetStreamAsync();

    Task SetStreamAsync(Stream data);

    Task<Stream> EchoStreamAsync(Stream data);

    Task<ComplexStream> GetComplexStreamAsync();

    /// <exception cref="TaskCanceledException"></exception>
    Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token);
}