using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using Stream = System.IO.Stream;

namespace Service
{
    internal class Service : IService
    {
        public void Hearbeat()
        {
            Console.WriteLine("[Hearbeat]");
        }

        [TestFilter]
        public void FilterAndHeader()
        {
            var h = NetRpcContext.ThreadHeader.Clone();
            Console.WriteLine($"[TestFilter], Header:{h["k1"]}");
        }

        public T2 CallByGenericType<T1, T2>(T1 obj)
        {
            Console.WriteLine($"[CallByGenericType], {obj}");
            return default;
        }

        public CustomObj SetAndGetObj(CustomObj obj)
        {
            var retObj = new CustomObj { Date = DateTime.Now, Name = "SetAndGetObj" };
            Console.WriteLine($"[SetAndGetObj], receive:{obj}, return:{retObj}");
            return retObj;
        }

        public void CallByCallBack(Action<CustomCallbackObj> cb)
        {
            Console.Write("[CallByCallBack]...");
            for (int i = 1; i <= 30; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                Task.Delay(100).Wait();
            }

            Console.WriteLine();
        }

        public void CallBySystemException()
        {
            Console.WriteLine("[CallBySystemException]...throw NotImplementedException");
            throw new NotImplementedException();
        }

        public void CallByCustomException()
        {
            Console.WriteLine("[CallBySystemException]...throw CustomException");
            throw new CustomException();
        }

        public Stream GetStream()
        {
            Console.WriteLine("[GetStream]...Send TestFile.txt");
            var stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public void SetStream(Stream data)
        {
            Console.WriteLine($"[SetStream]...length:{data.Length}, {TestHelper.Helper.ReadStr(data)}");
        }

        public Stream EchoStream(Stream data)
        {
            Console.WriteLine($"[EchoStream]...Received length:{data.Length}, {TestHelper.Helper.ReadStr(data)}...Send TestFile.txt");
            var stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public ComplexStream GetComplexStream()
        {
            Console.WriteLine("[GetComplexStream]...Send TestFile.txt...this is other info");
            return new ComplexStream
            {
                Stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }

        public ComplexStream ComplexCall(CustomObj obj, Stream data, Action<CustomCallbackObj> cb)
        {
            Console.Write($"[ComplexCall]...Received length:{data.Length}, {TestHelper.Helper.ReadStr(data)}, ");
            for (int i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj { Progress = i });
                Task.Delay(100).Wait();
            }
            Console.WriteLine("...Send TestFile.txt");
            return new ComplexStream
            {
                Stream = File.Open(TestHelper.Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }
    }
}