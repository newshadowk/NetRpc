using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task<string> CallAsync(string s);

        Task<string> Call2Async(string s, Func<int, Task> cb, CancellationToken token);

        Task Call3Async(Stream s, Action<int> cb);
    }
}