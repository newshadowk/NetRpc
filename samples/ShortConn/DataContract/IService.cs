using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;
using Newtonsoft.Json;

namespace DataContract;

[Tag("service")]
[HttpRoute("service")]
public interface IServiceAsync
{
    [HttpPost("call")]
    [FaultException(typeof(NotImplementedException), 400, "1", "this is a err")]
    Task<CallResult> CallAsync(CallParam p, Stream stream, Func<CbObj, Task> cb, CancellationToken token);

    Task<CbObj> Call2Async();
}

public class CbObj
{
    public string P1 { get; set; }
}

public interface IService1Async
{
    Task<string> Call1Async(string s);

    Task<Stream> Call2Async(string s);
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
    [JsonIgnore]
    public Stream Steam { get; set; }
}

//-----------------------------