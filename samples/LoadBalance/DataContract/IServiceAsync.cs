using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    Task CallAsync(Func<int, Task> cb, string s1);

    [MQPost]
    Task PostAsync(string s1, Stream stream);
}