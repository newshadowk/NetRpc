using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IService
{
    void Hearbeat();

    void FilterAndHeader();

    CustomObj SetAndGetObj(CustomObj obj);

    void CallByCallBack(Func<CustomCallbackObj, Task> cb);

    [FaultException(typeof(NotImplementedException))] //defined only for http channel
    void CallBySystemException();

    [FaultException(typeof(CustomException))] //defined only for http channel
    void CallByCustomException();

    Stream GetStream();

    void SetStream(Stream data);

    Stream EchoStream(Stream data);

    ComplexStream GetComplexStream();

    ComplexStream ComplexCall(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb);
}