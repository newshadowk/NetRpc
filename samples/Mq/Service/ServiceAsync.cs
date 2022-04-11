using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using NetRpc.RabbitMQ;
using Proxy.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service;

internal class ServiceAsync : IServiceAsync
{
    //private readonly QueueStatus _status;

    //public ServiceAsync(QueueStatusProvider provider)
    //{ 
    //    _status = provider.CreateQueueStatus("a1");
    //    _status = provider.CreateQueueStatus("a1");
    //}

    private static readonly HashSet<string> _ha = new();

    public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token)
    {
        Console.Write($"[ComplexCallAsync]...Received length:{data.Length}");
        MemoryStream ms = new();
        await data.CopyToAsync(ms);

        for (var i = 1; i <= 300; i++)
        {
            Console.Write($"{i}, ");
            await cb(new CustomCallbackObj {Progress = i});
            try
            {
                await Task.Delay(1000, token);
            }
            catch (Exception e)
            {
                Console.WriteLine("cancel!!!");
                throw;
            }
        }

        //if (!_ha.Contains(obj.Name))
        //{
        //    _ha.Add(obj.Name);
        //    throw new ArgumentNullException("123");
        //}

        Console.WriteLine("...Send TestFile.txt");
        return new ComplexStream
        {
            Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
            //Stream = File.Open(@"D:\TestFile\400MB.exe", FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
            OtherInfo = "this is other info"
        };
    }

    public async Task<string> Call2(string s)
    {
        //Console.WriteLine($"{_status.GetMainQueueMsgCount()}");
        Console.WriteLine($"Call2 {s}");
        return s;
    }

    public async Task P(CustomObj obj)
    {
        Console.WriteLine($"receive: {obj.Name}");
        await Task.Delay(5000);
        Console.WriteLine($"receive: {obj.Name} end");
    }
}