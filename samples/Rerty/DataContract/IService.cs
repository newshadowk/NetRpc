using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

[ClientRetry(new[] { typeof(FaultException<ArgumentException>) }, 1000, 1000)]
public interface IServiceAsync
{
    Task CallAsync(string s);

    Task Call2Async(Stream s);
}