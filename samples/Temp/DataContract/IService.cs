using System;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task<string> CallAsync(string s);

        Task<string> Call2Async(string s, Action<int> cb);
    }
}