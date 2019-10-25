using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Service
{
    internal class Service : IService
    {
        public Service()
        {
        }

        public void Hearbeat()
        {
            Console.WriteLine("[Hearbeat]");
        }

        [TestFilter]
        public void FilterAndHeader()
        {
            var h = GlobalActionExecutingContext.Context.Header;
            Console.WriteLine($"[TestFilter], Header:{h["k1"]}");
        }

        public CustomObj SetAndGetObj(CustomObj obj)
        {
            var retObj = new CustomObj {Date = DateTime.Now, Name = "SetAndGetObj"};
            Console.WriteLine($"[SetAndGetObj], receive:{obj}, return:{retObj}");
            return retObj;
        }

        public void CallByCallBack(Action<CustomCallbackObj> cb)
        {
            Console.Write("[CallByCallBack]...");
            for (var i = 1; i <= 3; i++)
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
            var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public void SetStream(Stream data)
        {
            Console.WriteLine($"[SetStream]...length:{data.Length}, {Helper.ReadStr(data)}");
        }

        public Stream EchoStream(Stream data)
        {
            Console.WriteLine($"[EchoStream]...Received length:{data.Length}, {Helper.ReadStr(data)}...Send TestFile.txt");
            var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream;
        }

        public ComplexStream GetComplexStream()
        {
            Console.WriteLine("[GetComplexStream]...Send TestFile.txt...this is other info");
            return new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }

        public ComplexStream ComplexCall(CustomObj obj, Stream data, Action<CustomCallbackObj> cb)
        {
            Console.Write($"[ComplexCall]...Received length:{data.Length}, {Helper.ReadStr(data)}, ");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                cb(new CustomCallbackObj {Progress = i});
                Task.Delay(100).Wait();
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