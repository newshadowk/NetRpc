using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Logging.Abstractions;
using NetRpc;
using NetRpc.Contract;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;
using NManager = NetRpc.RabbitMQ.NManager;

namespace Client
{
    internal class Program
    {
        private static ClientProxy<IService> _clientProxy;
        private static IService _proxy;
        private static IServiceAsync _proxyAsync;

        private static async Task Main(string[] args)
        {
            //RabbitMQ
            Console.WriteLine("--- Client RabbitMQ  ---");
            var mqF = new RabbitMQClientConnectionFactoryOptions(Helper.GetMQOptions(), NullLoggerFactory.Instance);
            _clientProxy = NManager.CreateClientProxy<IService>(mqF);
            _clientProxy.Connected += (s, e) => Console.WriteLine("[event] Connected");
            _clientProxy.DisConnected += (s, e) => Console.WriteLine("[event] DisConnected");
            _clientProxy.ExceptionInvoked += (s, e) => Console.WriteLine("[event] ExceptionInvoked");

            //Heartbeat
            _clientProxy.HeartbeatAsync += (s, e) =>
            {
                Console.WriteLine("[event] Heartbeat");
                ((IService) ((IClientProxy) s).Proxy).Hearbeat();
                return Task.CompletedTask;
            };
            //clientProxy.StartHeartbeat(true);

            _proxy = _clientProxy.Proxy;
            _proxyAsync = NManager.CreateClientProxy<IServiceAsync>(mqF).Proxy;
            RunTest();
            await RunTestAsync();

            //Grpc
            Console.WriteLine("\r\n--- Client Grpc  ---");
            var grpcF = new GrpcClientConnectionFactoryOptions(
                new GrpcClientOptions {Url = "http://localhost:50001"});
            _clientProxy = NetRpc.Grpc.NManager.CreateClientProxy<IService>(grpcF);
            _proxy = _clientProxy.Proxy;
            _proxyAsync = NetRpc.Grpc.NManager.CreateClientProxy<IServiceAsync>(grpcF).Proxy;

            RunTest();
            await RunTestAsync();

            Console.WriteLine("Test end.");
            Console.Read();
        }

        #region Test

        private static void RunTest()
        {
            Test_FilterAndHeader();
            Test_SetAndGetObj();
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
            _clientProxy.AdditionHeader.Add("k1", "header value");
            Console.Write("[FilterAndHeader], send:k1, header value");
            _proxy.FilterAndHeader();
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
            _proxy.CallByCallBack(async i => Console.Write(", " + i.Progress));
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
                    async i => Console.Write(", " + i.Progress));

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }

        #endregion

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
            //cts.Cancel();
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
}