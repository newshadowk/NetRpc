using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using TestHelper;

namespace Service
{
    public class ServiceAsync : IServiceAsync
    {
        public async Task<CustomObj> Call(string p1, int p2)
        {
            var retObj = new CustomObj {Date = DateTime.Now, Name = NameEnum.John};
            Console.WriteLine($"[Call]...receive:{p1}, {p2}, return:{retObj}");
            return retObj;
        }

        public async Task CallByCustomExceptionAsync()
        {
            Console.WriteLine("[CallByCustomExceptionAsync]...");
            throw new CustomException {P1 = "123", P2 = "abc"};
        }

        public async Task<Stream> EchoStreamAsync(Stream stream)
        {
            Console.WriteLine($"[EchoStreamAsync]...receive:streamLength:{stream.Length}");
            var ret = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return ret;
        }

        public async Task<ComplexStream> GetComplexStreamAsync()
        {
            Console.WriteLine("[GetComplexStreamAsync]...");
            return new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                StreamName = "TestFile.txt"
            };
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.WriteLine($"[ComplexCallAsync]...receive:{obj}, p1:{p1}, streamLength:{stream.Length}");

            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(100, token);
            }

            var ret = new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                StreamName = "TestFile.txt"
            };

            return ret;
        }
    }
}