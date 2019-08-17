using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;
using NetRpcManager = NetRpc.RabbitMQ.NetRpcManager;

namespace Client
{
    class Program
    {
        private static IService _proxy;
        private static IServiceAsync _proxyAsync;

        static void Main(string[] args)
        {
            //RabbitMQ
            Console.WriteLine("---  [RabbitMQ]  ---");
            var mqF = new RabbitMQClientConnectionFactoryOptions(Helper.GetMQOptions());
            var clientProxy = NetRpcManager.CreateClientProxy<IService>(mqF, false);
            clientProxy.Connected += (s, e) => Console.WriteLine("[event] Connected");
            clientProxy.DisConnected += (s, e) => Console.WriteLine("[event] DisConnected");
            clientProxy.ExceptionInvoked += (s, e) => Console.WriteLine("[event] ExceptionInvoked");

            //Heartbeat
            clientProxy.Heartbeat += async s =>
            {
                Console.WriteLine("[event] Heartbeat");
                s.Proxy.Hearbeat();
            };
            clientProxy.StartHeartbeat(true);

            _proxy = clientProxy.Proxy;
            _proxyAsync = NetRpcManager.CreateClientProxy<IServiceAsync>(mqF, false).Proxy;
            RunTest();
            RunTestAsync().Wait();

            //Grpc
            Console.WriteLine("\r\n--- [Grpc]  ---");
            var grpcF = new GrpcClientConnectionFactoryOptions(
                new GrpcClientOptions { Channel = new Channel("localhost", 50001, ChannelCredentials.Insecure) });
            _proxy = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>(grpcF).Proxy;
            _proxyAsync = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IServiceAsync>(grpcF).Proxy;
            RunTest();
            RunTestAsync().Wait();

            Console.WriteLine("Test end.");
            Console.Read();
        }

        #region Test

        private static void RunTest()
        {
            Test_FilterAndHeader();
            Test_SetAndGetObj();
            Test_CallByGeneric();
            Test_CallByCallBack();
            Test_CallBySystemException();
            Test_CallByCustomException();
            Test_GetStream();
            Test_SetStream();
            Test_EchoStream();
            Test_GetComplexStream();
            Test_ComplexCall();
        }

        private static void Test_FilterAndHeader()
        {
            NetRpcContext.ThreadHeader.CopyFrom(new Dictionary<string, object> {{"k1", "header value"}});
            Console.Write("[FilterAndHeader], send:k1, header value");
            _proxy.FilterAndHeader();
        }

        private static void Test_CallByGeneric()
        {
            var obj = new CustomObj {Date = DateTime.Now, Name = "test"};
            Console.Write($"[CallByGeneric], send:{obj}, receive:");
            var ret = _proxy.CallByGenericType<CustomObj, int>(obj);
            Console.WriteLine($"{ret}");
        }

        private static void Test_SetAndGetObj()
        {
            var obj = new CustomObj {Date = DateTime.Now, Name = "test"};
            Console.Write($"[SetAndGetObj], send:{obj}, ");
            var ret = _proxy.SetAndGetObj(obj);
            Console.WriteLine($"receive:{ret}");
        }

        private static void Test_CallByCallBack()
        {
            Console.Write("[CallByCallBack]");
            _proxy.CallByCallBack(i => Console.Write(", " + i.Progress));
            Console.WriteLine();
        }

        private static void Test_CallBySystemException()
        {
            Console.Write("[CallBySystemException]...");
            try
            {
                _proxy.CallBySystemException();
            }
            catch (FaultException<NotImplementedException> e)
            {
                Console.WriteLine($"catch FaultException<NotImplementedException> {e}");
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("catch NotImplementedException");
            }
            catch (Exception)
            {
            }
        }

        private static void Test_CallByCustomException()
        {
            Console.Write("[CallByCustomException]...");
            try
            {
                _proxy.CallByCustomException();
            }
            catch (FaultException<CustomException>)
            {
                Console.WriteLine("catch FaultException<CustomException>");
            }
            catch (CustomException)
            {
                Console.WriteLine("catch CustomException");
            }
        }

        private static void Test_GetStream()
        {
            Console.Write("[GetStream]...");
            using (var stream = _proxy.GetStream())
                Console.WriteLine($"length:{stream.Length}, {Helper.ReadStr(stream)}");
        }

        private static void Test_SetStream()
        {
            Console.Write("[SetStream]...Send TestFile.txt");
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                _proxy.SetStream(stream);
        }

        private static void Test_EchoStream()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[EchoStream]...Send TestFile.txt...");
                var data = _proxy.EchoStream(stream);
                Console.WriteLine($"Received length:{data.Length}, {Helper.ReadStr(data)}");
            }
        }

        private static void Test_GetComplexStream()
        {
            Console.Write("[GetComplexStream]...");
            var complexStream = _proxy.GetComplexStream();
            using (var stream = complexStream.Stream)
                Console.WriteLine($"length:{stream.Length}, {Helper.ReadStr(stream)}");
            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }

        private static void Test_ComplexCall()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCall]...Send TestFile.txt...");
                var complexStream = _proxy.ComplexCall(
                    new CustomObj {Date = DateTime.Now, Name = "ComplexCall"},
                    stream,
                    i => Console.Write(", " + i.Progress));

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }

        #endregion

        #region TestAsync

        private static async Task RunTestAsync()
        {
            await Test_CallByGenericAsync();
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

        private static async Task Test_CallByGenericAsync()
        {
            var obj = new CustomObj {Date = DateTime.Now, Name = "test"};
            Console.Write($"[CallByGenericAsync], send:{obj}, ");
            var ret = await _proxyAsync.CallByGenericAsync<CustomObj, int>(obj);
            Console.WriteLine($"receive:{ret}");
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
            catch (TaskCanceledException)
            {
                Console.WriteLine("canceled.");
            }
        }

        private static async Task Test_CallByCallBackAsync()
        {
            Console.Write("[CallByCallBackAsync]");
            await _proxyAsync.CallByCallBackAsync(i => Console.Write(", " + i.Progress));
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
            catch (NotImplementedException)
            {
                Console.WriteLine("catch NotImplementedException");
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
            catch (CustomException)
            {
                Console.WriteLine("catch CustomException");
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
                    i => Console.Write(", " + i.Progress),
                    default);

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }

        #endregion
    }
}