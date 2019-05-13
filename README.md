# RabbitMQ.Rpc

RabbitMq.Rpc is a light weight rpc engine base on RabbitMQ targeting .NET Standard.  It use the simple interface to call each other, contains the load balance mode.

## Hello world!
```c#
//service side
var p = new MQParamEx(user, password, host, virtualHost, port, queue);
var proxy = RpcManager.CreateServiceProxy<IService>(p, new Service());
proxy.Open();

class Service : IService
{
    public void Call(string s)
    {
        Console.WriteLine($"Receive: {s}");
    }
}

//client side
var p = new MQParamEx(user, password, host, virtualHost, port, queue);
var proxy = RpcManager.CreateClientProxy<IService>(p);
proxy.Connect();
proxy.Proxy.Call("hello world!");
```

## Supported interface type
```c#
public interface IService
{
    #region Sync

    void SetObj(CustomObj obj);

    CustomObj GetObj();

    void CallByCallBack(Action<CustomCallbackObj> cb);

    /// <exception cref="NotImplementedException"></exception>
    void CallBySystemException();

    /// <exception cref="CustomException"></exception>>
    void CallByCustomException();

    Stream GetStream();

    void SetStream(Stream data);

    Stream EchoStream(Stream data);

    ComplexStream GetComplexStream();

    ComplexStream ComplexCall(CustomObj obj, Stream data, Action<CustomCallbackObj> cb);

    #endregion

    #region Async

    Task SetObjAsync(CustomObj obj);

    Task<CustomObj> GetObjAsync();

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

    #endregion
}
```

## Sync/Async

Can use the both Sync/Async ways to defines the interface.

```c#
void SetObj(CustomObj obj);
Task SetObjAsync(CustomObj obj);
```

## Load Balance

When run multiple service instances, ther service will auto apply the load balance, this function is base on the RabbitMQ.

## Pass Exception

Can pass the exception via the interface, on the client side, just use the try {} catch {} block.

```c#
/// <exception cref="CustomException"></exception>>
Task CallByCustomExceptionAsync();

try
{
     await proxy.CallByCustomExceptionAsync();
}
catch (CustomException e)
{
    //handling exception
}
```
## Cancel

```c#
/// <exception cref="TaskCanceledException"></exception>
Task CallByCancelAsync(CancellationToken token);
```

## Call back

```c#
Task CallByCallBackAsync(Action<CustomCallbackObj> cb);
```

## Stream

```c#
Task<Stream> GetStreamAsync();

Task SetStreamAsync(Stream data);

Task<Stream> EchoStreamAsync(Stream data);
```

## Samples

* [Hello World](samples/HelloWorld)

* [Base](samples/Base)

* [LoadBalance](samples/LoadBalance)
