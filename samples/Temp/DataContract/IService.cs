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

        Task<RetObj> Call3Async(Stream s, Func<int, Task> cb, CancellationToken token);
    }

    [Serializable]
    public class RetObj
    {
        [field: NonSerialized]
        public Stream Stream { get; set; }
        public string Name { get; set; }
    }

    public interface IClientService
    {
        Task<string> CallAsync(string s);

        Task<string> Call2Async(string s, Func<int, Task> cb, CancellationToken token);

        Task<RetObj> Call3Async(Stream s, Func<int, Task> cb, CancellationToken token);
    }
}