using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;

namespace Service
{
    public class ServiceAsync : IServiceAsync
    {
        public async Task<CustomObj> Call(string p1, int p2, Guid p3, DateTime p4)
        {
            var retObj = new CustomObj { Date = DateTime.Now, Name = "Call" };
            Console.WriteLine($"[Call]...receive:{p1}, {p2}, {p3}, {p4} return:{retObj}");
            return retObj;
        }

        public async Task<Stream> EchoStreamAsync(string p1, Stream data)
        {
            Console.WriteLine($"[EchoStreamAsync]...Received length:{data.Length}, {p1}, {TestHelper.Helper.ReadStr(data)}...Send TestFile.txt");
            var stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.WriteLine($"[ComplexCallAsync]...receive:{obj}, p1:{p1}, streamLength:{stream.Length}");

            for (int i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj { Progress = i });
                await Task.Delay(100, token);
            }

            ComplexStream ret = new ComplexStream
            {
                Stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                StreamName = "TestFile.txt"
            };

            return ret;
        }
    }
}