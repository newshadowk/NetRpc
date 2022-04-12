using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.Contract;
using Helper = TestHelper.Helper;
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable PossibleNullReferenceException

namespace Client;

internal class Program
{
    private static IClientProxy<IServiceAsync> _clientProxy;
    private static IServiceAsync _proxyAsync;

    private static async Task Main(string[] args)
    {
        await RabbitMQ();
        await Grpc();
        await Http();

        Console.WriteLine("\r\n--------------- End ---------------");
        Console.Read();
    }

    private static async Task RabbitMQ()
    {
        //RabbitMQ
        Console.WriteLine("\r\n--------------- Client RabbitMQ ---------------");
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddLogging(configure => configure.AddConsole());
        services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
        var sp = services.BuildServiceProvider();
        //_clientProxy = sp.GetService<IClientProxy<IServiceAsync>>();
        //_clientProxy.Connected += (_, _) => Console.WriteLine("[event] Connected");
        //_clientProxy.DisConnected += (_, _) => Console.WriteLine("[event] DisConnected");
        //_clientProxy.ExceptionInvoked += (_, _) => Console.WriteLine("[event] ExceptionInvoked");

        ////Heartbeat
        //_clientProxy.HeartbeatAsync += async (s, e) =>
        //{
        //    Console.WriteLine("[event] Heartbeat"); 
        //    await ((IServiceAsync)((IClientProxy)s).Proxy).Hearbeat();
        //};
        //_clientProxy.StartHeartbeat(true);

        _proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
        await RunTestAsync();
    }

    private static async Task Grpc()
    {
        Console.WriteLine("\r\n--------------- Client Grpc ---------------");
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddNGrpcClient(o => o.Url = "http://localhost:50001");
        var sp = services.BuildServiceProvider();
        _proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
        await RunTestAsync();
    }

    private static async Task Http()
    {
        Console.WriteLine("\r\n--------------- Client Http ---------------");
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddNHttpClient(o =>
        {
            o.SignalRHubUrl = "http://localhost:50002/callback";
            o.ApiUrl = "http://localhost:50002/api";
        });
        var sp = services.BuildServiceProvider();
        _proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
        await RunTestAsync();
    }

    #region TestAsync

    private static async Task RunTestAsync()
    {
        await Test_SetAndGetObjAsync();
        await Test_CallByCancelAsync();
        await Test_CallByCallBackAsync();
        await Test_CallBySystemExceptionAsync();
        await Test_CallByCustomExceptionAsync();
        await Test_GetStreamAsync();
        await Test_SetStreamAsync();
        await Test_EchoStreamAsync();
        await Test_GetComplexStreamAsync();
        await Test_ComplexCallAsync();
    }

    private static async Task Test_SetAndGetObjAsync()
    {
        var obj = new CustomObj {Date = DateTime.Now, Name = "test"};
        Console.Write($"[SetAndGetObjAsync], send:{obj}, ");
        var ret = await _proxyAsync.SetAndGetObj(obj);
        Console.WriteLine($"receive:{ret}");
    }

    private static async Task Test_CallByCancelAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(500);
        try
        {
            Console.Write("[CallWithCancelAsync], cancel after 500 ms, ");
            await _proxyAsync.CallByCancelAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("canceled.");
        }
    }

    private static async Task Test_CallByCallBackAsync()
    {
        Console.Write("[CallByCallBackAsync]");
        await _proxyAsync.CallByCallBackAsync(async i => Console.Write(", " + i.Progress));
        Console.WriteLine();
    }

    private static async Task Test_CallBySystemExceptionAsync()
    {
        Console.Write("[CallBySystemExceptionAsync]...");
        try
        {
            await _proxyAsync.CallBySystemExceptionAsync();
        }
        catch (FaultException<NotImplementedException> e)
        {
            Console.WriteLine($"catch FaultException<NotImplementedException> {e}");
        }
    }

    private static async Task Test_CallByCustomExceptionAsync()
    {
        Console.Write("[CallByCustomExceptionAsync]...");
        try
        {
            await _proxyAsync.CallByCustomExceptionAsync();
        }
        catch (FaultException<CustomException>)
        {
            Console.WriteLine("catch FaultException<CustomException>");
        }
    }

    private static async Task Test_GetStreamAsync()
    {
        Console.Write("[GetStreamAsync]...");
        using (var stream = await _proxyAsync.GetStreamAsync())
            Console.WriteLine($"length:{stream.Length}, {Helper.ReadStr(stream)}");
    }

    private static async Task Test_SetStreamAsync()
    {
        Console.Write("[SetStreamAsync]...Send TestFile.txt");
        using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            await _proxyAsync.SetStreamAsync(stream);
    }

    private static async Task Test_EchoStreamAsync()
    {
        using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.Write("[EchoStreamAsync]...Send TestFile.txt...");
            var data = await _proxyAsync.EchoStreamAsync(stream);
            Console.WriteLine($"Received length:{stream.Length}, {Helper.ReadStr(data)}");
        }
    }

    private static async Task Test_GetComplexStreamAsync()
    {
        Console.Write("[GetComplexStreamAsync]...");
        var complexStream = await _proxyAsync.GetComplexStreamAsync();
        using (var stream = complexStream.Stream)
            Console.WriteLine($"length:{stream.Length}, {Helper.ReadStr(stream)}");
        Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
    }

    private static async Task Test_ComplexCallAsync()
    {
        using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
            var complexStream = await _proxyAsync.ComplexCallAsync(
                new CustomObj {Date = DateTime.Now, Name = "ComplexCall"},
                stream,
                async i => Console.Write(", " + i.Progress),
                default);

            using (var stream2 = complexStream.Stream)
                Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }
    }

    #endregion
}