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

[Tag("service")]
[HttpRoute("service")]
public interface IService_
{
    [HttpPost("call/start")]
    Task<string> CallAsync(CallParam p, Stream stream);

    [HttpGet("call/prog/{id}")]
    Task<ContextData> CallProgressAsync(string id);

    [HttpGet("call/cancel/{id}")]
    Task CallCancel(string id);

    [HttpGet("call/result/{id}")]
    Task<CallResult> CallResultAsync(string id);
}
