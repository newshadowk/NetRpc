using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using TestHelper;

namespace Service
{
    internal class ServiceAsync : IServiceAsync
    {
        public async Task<T2> CallByGenericAsync<T1, T2>(T1 obj)
        {
            var ret = new CustomObj {Date = DateTime.Now, Name = "CallByGenericAsync"};
            Console.WriteLine($"[CallByGenericAsync], receive:{obj}");
            return default;
        }

        public async Task<CustomObj> SetAndGetObj(CustomObj obj)
        {
            var ret = new CustomObj {Date = DateTime.Now, Name = "SetAndGetObj"};
            Console.WriteLine($"[SetAndGetObj], receive:{obj}, return:{ret}");
            return ret;
        }

        public async Task CallByCallBackAsync(Action<CustomCallbackObj> cb)
        {
            Console.Write("[CallByCallBackAsync]...");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(100);
            }

            Console.WriteLine();
        }

        public async Task CallByCancelAsync(CancellationToken token)
        {
            Console.WriteLine("[CallByCancelAsync]...");
            await Task.Delay(10000, token);
        }

        public async Task CallByCustomExceptionAsync()
        {
            Console.WriteLine("[CallByCustomExceptionAsync]...throw CustomException");
            throw new CustomException();
        }

        public async Task CallBySystemExceptionAsync()
        {
            Console.WriteLine("[CallBySystemExceptionAsync]...throw NotImplementedException");
            throw new NotImplementedException();
        }

        public async Task<Stream> GetStreamAsync()
        {
            Console.WriteLine("[GetStreamAsync]...Send TestFile.txt");
            var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public async Task SetStreamAsync(Stream data)
        {
            Console.WriteLine($"[SetStreamAsync]...length:{data.Length}, {Helper.ReadStr(data)}");
        }

        public async Task<Stream> EchoStreamAsync(Stream data)
        {
            Console.WriteLine($"[EchoStreamAsync]...Received length:{data.Length}, {Helper.ReadStr(data)}...Send TestFile.txt");
            var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public async Task<ComplexStream> GetComplexStreamAsync()
        {
            Console.WriteLine("[GetComplexStreamAsync]...Send TestFile.txt...this is other info");
            return new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.Write($"[ComplexCallAsync]...Received length:{data.Length}, {Helper.ReadStr(data)}, ");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(100, token);
            }

            Console.WriteLine("...Send TestFile.txt");
            return new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }
    }
}