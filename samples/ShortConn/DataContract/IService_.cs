using System.IO;
using System.Threading.Tasks;
using NetRpc;
using NetRpc.Contract;

namespace DataContract;

[Tag("service")]
[HttpRoute("service")]
public interface IService_
{
    [HttpPost("call/start")]
    Task<string> CallAsync(CallParam p, Stream stream);

    [HttpGet("call/result/{id}")]
    [FaultException(typeof(IdNotFoundException), 400, "1")]
    Task<CallResult> CallResultAsync(string id);

    [HttpGet("call2/start")]
    Task<string> Call2Async();

    [HttpGet("call2/result/{id}")]
    [FaultException(typeof(IdNotFoundException), 400, "1")]
    Task<CbObj?> Call2ResultAsync(string id);
}