using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;

namespace Service
{
    public class Service : IServiceAsync
    {
        public async Task<CustomObj> Call(string p1, string p2)
        {
            var retObj = new CustomObj { Date = DateTime.Now, Name = "Call" };
            Console.WriteLine($"[Call]...receive:{p1}, {p2}, return:{retObj}");
            return retObj;
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