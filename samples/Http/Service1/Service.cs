using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using NetRpc.Http.Client;
using Helper = TestHelper.Helper;

namespace Service
{
    public class ServiceAsync : IServiceAsync
    {
        public Task<CustomObj> Call2Async(CObj obj, string s1, string s2)
        {
            throw new NotImplementedException();
        }

        public async Task<CustomObj> CallAsync(string p1, int p2)
        {
            var retObj = new CustomObj { Date = DateTime.Now, Name = NameEnum.John };
            var h = GlobalActionExecutingContext.Context.Header;
            //Console.WriteLine($"[Call]...receive:{p1}, {p2}, h1:{h["h1"]}, h2:{h["h2"]} return:{retObj}");
            Console.WriteLine($"[Call]...receive:{p1}, {p2}, return:{retObj}");
            return retObj;
        }

        public async Task CallByCustomExceptionAsync()
        {
            Console.WriteLine("[CallByCustomExceptionAsync]...");
            //throw new CustomException {P1 = "123", P2 = "abc"};
            throw new CustomException2();
        }

        public async Task CallByDefaultExceptionAsync()
        {
            Console.WriteLine("[CallByDefaultExceptionAsync]...");
            throw new NotImplementedException("2311");
        }

        public async Task CallByCancelAsync(CancellationToken token)
        {
            for (var i = 1; i <= 10; i++)
            {
                Console.Write($"{i}, waiting cancel...");
                await Task.Delay(1000, token);
            }
        }

        public async Task CallByResponseTextExceptionAsync()
        {
            throw new ResponseTextException("\"this is customs text.\"", 701);
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.WriteLine($"[ComplexCallAsync]...receive:{obj}, p1:{p1}, streamLength:{stream.Length}");

            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj { Progress = i });
                await Task.Delay(1000, token);
            }

            var ret = new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                StreamName = "TestFile.txt",
                InnerObj = new InnerObj()
                {
                    CustomObj = new CustomObj()
                    {
                        Name = NameEnum.John
                    },
                    P1 = "中文p1!@#$%^&*()_+\"\":?~!@#$"
                }
            };

            return ret;
        }

        public async Task<string> ComplexCall2Async(Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.WriteLine($"[ComplexCallAsync]...");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj { Progress = i });
                await Task.Delay(1000, token);
            }
            Console.WriteLine($"[ComplexCallAsync]...end");
            return "ret0";
        }
    }
}