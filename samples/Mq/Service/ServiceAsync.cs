using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using TestHelper;

namespace Service;

internal class ServiceAsync : IServiceAsync
{
    public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token)
    {
        //Console.Write($"[ComplexCallAsync]...Received length:{data.Length}, {Helper.ReadStr(data)}, ");
        Console.Write($"[ComplexCallAsync]...Received length:{data.Length}");
        for (var i = 1; i <= 5000; i++)
        {
            Console.Write($"{i}, ");
            await cb(new CustomCallbackObj {Progress = i});
            try
            {
                await Task.Delay(2000, token);
            }
            catch (Exception e)
            {
                Console.WriteLine("cancel!!!");
                throw;
            }
        }

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
        Console.WriteLine($"Call2 {s}");
        return s;
    }
}