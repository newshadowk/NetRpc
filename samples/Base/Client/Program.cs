using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Nrpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        private static IService _proxy;
        private static IServiceAsync _proxyAsync;

        static void Main(string[] args)
        {
            Console.WriteLine("---  [RabbitMQ]  ---");
            var mqF = new ClientConnectionFactory(Helper.GetMQParam());
            _proxy = NRpcManager.CreateClientProxy<IService>(mqF).Proxy;
            _proxyAsync = NRpcManager.CreateClientProxy<IServiceAsync>(mqF).Proxy;
            RunTest();
            RunTestAsync().Wait();

            Console.WriteLine("\r\n--- [Grpc]  ---");
            var grpcF = new Nrpc.Grpc.ClientConnectionFactory("localhost", 50001);
            _proxy = Nrpc.Grpc.NRpcManager.CreateClientProxy<IService>(grpcF).Proxy;
            _proxyAsync = Nrpc.Grpc.NRpcManager.CreateClientProxy<IServiceAsync>(grpcF).Proxy;
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
            Nrpc.NrpcContext.ThreadHeader.CopyFrom(new Dictionary<string, object> { { "k1", "header value" } });
            Console.Write("[FilterAndHeader], send:k1, header value");
            _proxy.FilterAndHeader();
        }

        private static void Test_CallByGeneric()
        {
            CustomObj obj = new CustomObj { Date = DateTime.Now, Name = "test" };
            Console.Write($"[CallByGeneric], send:{obj}, receive:");
            var ret = _proxy.CallByGeneric<CustomObj, int>(obj);
            Console.WriteLine($"{ret}");
        }

        private static void Test_SetAndGetObj()
        {
            CustomObj obj = new CustomObj {Date = DateTime.Now, Name = "test"};
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
            catch (CustomException)
            {
                Console.WriteLine("catch CustomException");
            }
        }

        private static void Test_GetStream()
        {
            Console.Write("[GetStream]...");
            using (var stream = _proxy.GetStream())
                Console.WriteLine(Helper.ReadStr(stream));
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
                Console.WriteLine($"Received:{Helper.ReadStr(data)}");
            }
        }

        private static void Test_GetComplexStream()
        {
            Console.Write("[GetComplexStream]...");
            var complexStream = _proxy.GetComplexStream();
            using (var stream = complexStream.Stream)
                Console.Write(Helper.ReadStr(stream));
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
                    Console.Write($", receive:{Helper.ReadStr(stream2)}");
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
            CustomObj obj = new CustomObj { Date = DateTime.Now, Name = "test" };
            Console.Write($"[CallByGenericAsync], send:{obj}, ");
            var ret = await _proxyAsync.CallByGenericAsync<CustomObj, int>(obj);
            Console.WriteLine($"receive:{ret}");
        }

        private static async Task Test_SetAndGetObjAsync()
        {
            CustomObj obj = new CustomObj {Date = DateTime.Now, Name = "test"};
            Console.Write($"[SetAndGetObjAsync], send:{obj}, ");
            var ret = await _proxyAsync.SetAndGetObj(obj);
            Console.WriteLine($"receive:{ret}");
        }

        private static async Task Test_CallByCancelAsync()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(2000);
            try
            {
                Console.Write("[CallWithCancelAsync], cancel after 2000 ms, ");
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
            catch (CustomException)
            {
                Console.WriteLine("catch CustomException");
            }
        }

        private static async Task Test_GetStreamAsync()
        {
            Console.Write("[GetStreamAsync]...");
            using (var stream = await _proxyAsync.GetStreamAsync())
                Console.WriteLine(Helper.ReadStr(stream));
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
                Console.WriteLine($"Received:{Helper.ReadStr(data)}");
            }
        }

        private static async Task Test_GetComplexStreamAsync()
        {
            Console.Write("[GetComplexStreamAsync]...");
            var complexStream = await _proxyAsync.GetComplexStreamAsync();
            using (var stream = complexStream.Stream)
                Console.Write(Helper.ReadStr(stream));
            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }

        private static async Task Test_ComplexCallAsync()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
                var complexStream = await _proxyAsync.ComplexCallAsync(
                    new CustomObj { Date = DateTime.Now, Name = "ComplexCall" },
                    stream,
                    i => Console.Write(", " + i.Progress), 
                    default);

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive:{Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }

        #endregion
    }
}