using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc;

namespace DataContract
{
    public interface IServiceAsync
    {
        Task CallAsync(Action<int> cb, string s1);

        [MQPost]
        Task PostAsync(string s1, Stream stream);
    }
}