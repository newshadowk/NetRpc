using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using NetRpc.Contract;
using NetRpc.Http.Client;
using Helper = TestHelper.Helper;

namespace Service
{
    public class Service3Async : IService3Async
    {
        public async Task CallAsync()
        {
            Console.WriteLine("CallAsync");
        }

        public async Task Call2Async()
        {
            Console.WriteLine("Call2Async");
        }

        public async Task Call3Async()
        {
            Console.WriteLine("Call3Async");
        }

        public async Task Call4Async()
        {
            Console.WriteLine("Call4Async");
        }
    }

    public class Service2Async : IService2Async
    {
        public async Task<string> Call1Async(string p1, int p2)
        {
            var s = $"[Call1Async]...{p1}, {p2}";
            Console.WriteLine(s);
            return s;
        }

        public async Task<string> Call2Async(CallObj obj)
        {
            var s = $"[Call2Async]...{obj.P1}, {obj.P2}";
            Console.WriteLine(s);
            return s;
        }

        public async Task<string> Call3Async(CallObj obj, string s1)
            //public async Task<string> Call3Async(CallObj obj)
        {
            var s = $"[Call3Async]...{obj.P1}, {obj.P2}";
            Console.WriteLine(s);
            return s;
        }

        public Task<string> Call3Async(CallObj obj)
        {
            throw new NotImplementedException();
        }

        public Task<string> Call3Async(CallObj obj, string s1, Func<double, Task> cb, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Call4Async(CallObj obj, Func<double, Task> cb, CancellationToken token)
        {
            var s = $"[Call4Async]...{obj.P1}, {obj.P2}";

            for (var i = 0; i < 50; i++)
            {
                await cb(i);
                await Task.Delay(100, token);
            }

            Console.WriteLine(s);
            return s;
        }

        public async Task<string> Call5Async(string p1, int p2, Func<double, Task> cb, CancellationToken token)
        {
            var s = $"[Call5Async]...{p1}, {p2}";
            Console.WriteLine(s);
            return s;
        }

        public async Task<CObj> CallNone(CObj obj, Func<double, Task> cb, Stream stream)
        {
            var ss = stream as ProxyStream;
            //ss.ProgressAsync += async (s, e) =>
            //{
            //    Console.WriteLine(e.Value);
            //    await cb(e.Value);
            //};
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return new CObj();
        }

        public async Task Call0Async(Guid taskId)
        {
            Console.WriteLine(taskId);
        }

        public Task<string> CallNone2(CObj obj)
        {
            throw new NotImplementedException();
        }

        public Task<string> CallNone3()
        {
            throw new NotImplementedException();
        }
    }

    public class ServiceAsync : IServiceAsync
    {
        public Task<CustomObj> Call2Async(CObj obj, string s1, string s2)
        {
            throw new NotImplementedException();
        }

        public async Task<CustomObj> CallAsync(string p1, int p2)
        {
            var retObj = new CustomObj {Date = DateTime.Now, Name = NameEnum.John};
            var h = GlobalActionExecutingContext.Context.Header;
            //Console.WriteLine($"[Call]...receive:{p1}, {p2}, h1:{h["h1"]}, h2:{h["h2"]} return:{retObj}");
            Console.WriteLine($"[Call]...receive:{p1}, {p2}, return:{retObj}");
            return retObj;
        }

        public Task Call3Async(SimObj obj)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Call4Async(string p1, int p2)
        {
            throw new NotImplementedException();
        }

        public async Task CallByCustomExceptionAsync()
        {
            Console.WriteLine("[CallByCustomExceptionAsync]...");
            //throw new CustomException {P1 = "123", P2 = "abc"};
            //throw new CustomException2();
            throw new FaultException<CustomException2>(new CustomException2());
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

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Func<CustomCallbackObj, Task> cb, CancellationToken token)
        {
            Console.WriteLine($"[ComplexCallAsync]...receive:{obj}, p1:{p1}, streamLength:{stream.Length}");

            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                await cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(1000, token);
            }

            var ret = new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                StreamName = "TestFile.txt",
                InnerObj = new InnerObj
                {
                    CustomObj = new CustomObj
                    {
                        Name = NameEnum.John
                    },
                    P1 = "中文p1!@#$%^&*()_+\"\":?~!@#$"
                }
            };

            return ret;
        }

        public async Task<int> UploadAsync(Stream stream, string streamName, string p1, Func<int, Task> cb, CancellationToken token)
        {
            Console.WriteLine($"UploadAsync, {p1}");
            var path = @"d:\testfile\tgt.rar";
            File.Delete(path);
            var ms = new MemoryStream();
            ((ProxyStream) stream).CacheStream = ms;

            using (var fs = File.OpenWrite(path))
            {
                const int ReadBuffSize = 81920;
                var buffer = new byte[ReadBuffSize];
                var readCount = await stream.ReadAsync(buffer, 0, ReadBuffSize, token);
                while (readCount > 0)
                {
                    await fs.WriteAsync(buffer, 0, readCount, token);
                    readCount = await stream.ReadAsync(buffer, 0, ReadBuffSize, token);
                }
            }

            ms.Seek(0, SeekOrigin.Begin);

            using var ws = File.OpenWrite(@"d:\testfile\tgt2.rar");
            await ms.CopyToAsync(ws);

            return 100;
        }

        public async Task<string> ComplexCall2Async(Action<CustomCallbackObj> cb, CancellationToken token)
        {
            Console.WriteLine("[ComplexCallAsync]...");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(1000, token);
            }

            Console.WriteLine("[ComplexCallAsync]...end");
            return "ret0";
        }
    }

    public class Service4Async : IService4Async
    {
        public async Task<Obj4> Call(Obj4 obj)
        {
            //id.dt = DateTime.Now;
            //id.dt2 = DateTimeOffset.Now;
            return new();
        }

        public async Task Call2(string testRed1, string testRed2)
        {
            Console.WriteLine($"{testRed1}, {testRed2}");
        }
    }
}