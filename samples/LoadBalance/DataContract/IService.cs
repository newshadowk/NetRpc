using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc;

namespace DataContract
{
    public interface IService
    {
        Task CallAsync(Action<int> cb, string s1);

        [NetRpcPost]
        Task PostAsync(string s1, Stream stream);
    }
}