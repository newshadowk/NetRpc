using System.IO;
using System.Threading.Tasks;
using NetRpc;
using NetRpc.Contract;

namespace DataContract;

[Tag("service")]
[HttpRoute("service")]
[FaultExceptionDefine(typeof(IdNotFoundException), 400, "1")]
[InheritedFaultExceptionDefine]
public interface IService_
{
    [HttpPost("call/start")]
    Task<string> CallAsync(CallParam p, Stream stream);

    [HttpGet("call/result/{id}")]
    Task<CallResult> CallResultAsync(string id);

    [HttpGet("call2/start")]
    Task<string> Call2Async();

    [HttpGet("call2/result/{id}")]
    Task<CbObj?> Call2ResultAsync(string id);
}