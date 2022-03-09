using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

[Tag("service")]
[HttpRoute("service")]
public interface IServiceAsync
{
    [HttpPost("call")]
    [FaultException(typeof(NotImplementedException), 400, "1", "this is a err")]
    Task<CallResult> CallAsync(CallParam p, Stream stream, Func<double, Task> cb, CancellationToken token);
}

public interface IService1Async
{
    Task<string> Call1Async(string s);
}

[Serializable]
public class CallParam
{
    public string StreamName { get; set; }
    public string P1 { get; set; }
}

[Serializable]
public class CallResult
{
    public string P1 { get; set; }
    
    public string StreamName { get; set; }

    [field: NonSerialized]
    public Stream Steam { get; set; }
}

//-----------------------------