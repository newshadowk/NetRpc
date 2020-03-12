using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using NetRpc.Http.Client;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        private static IServiceAsync _proxyAsync;

        static async Task Main(string[] args)
        {
            _proxyAsync = NetRpcManager.CreateClientProxy<IServiceAsync>(new HttpClientOptions
            {
                SignalRHubUrl = "http://localhost:5000/callback",
                ApiUrl = "http://localhost:5000/api"
            }).Proxy;

            await Test_CallAsync();
            await Test_CallByCancelAsync();
            await Test_CallByCustomExceptionAsync();
            await Test_CallByDefaultExceptionAsync();
            await Test_CallByResponseTextExceptionAsync();
            await Test_ComplexCallAsync();

            Console.Read();
        }

        private static async Task Test_CallAsync()
        {
            Console.WriteLine("[CallAsync]...123, 123");
            await _proxyAsync.CallAsync("123", 123);
        }

        private static async Task Test_CallByCancelAsync()
        {
            Console.Write("[CallByCancelAsync]...");
            var cts = new CancellationTokenSource();
            cts.CancelAfter(2000);
            try
            {
                await _proxyAsync.CallByCancelAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("cancelled.");
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
            catch (FaultException<CustomException2>)
            {
                Console.WriteLine("catch FaultException<CustomException2>");
            }
        }

        private static async Task Test_CallByDefaultExceptionAsync()
        {
            Console.Write("[CallByDefaultExceptionAsync]...");

            try
            {
                await _proxyAsync.CallByDefaultExceptionAsync();
            }
            catch (FaultException)
            {
                Console.WriteLine("catch FaultException");
            }
        }

        private static async Task Test_CallByResponseTextExceptionAsync()
        {
            Console.Write("[CallByResponseTextExceptionAsync]...");

            try
            {
                await _proxyAsync.CallByResponseTextExceptionAsync();
            }
            catch (ResponseTextException e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task Test_ComplexCallAsync()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCallAsync]...Send TestFile.txt...");

                var complexStream = await _proxyAsync.ComplexCallAsync(
                    new CustomObj {Date = DateTime.Now, Name = NameEnum.John},
                    "123",
                    stream,
                    async i => Console.Write(", " + i.Progress),
                    default);
                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", innerObj:{complexStream.InnerObj}");
            }
        }
    }
}