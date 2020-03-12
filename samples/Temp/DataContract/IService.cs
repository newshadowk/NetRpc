using System;
using System.IO;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task<string> CallAsync(string s);

        Task<string> Call2Async(string s, Func<int, Task> cb);

        Task Call3Async(Stream s, Action<int> cb);
    }
}