using System.IO;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

[Tag("service")]
[HttpRoute("service")]
public interface IService_
{
    [HttpPost("call/start")]
    Task<string> CallAsync(CallParam p, Stream stream);

    [HttpGet("call/result/{id}")]
    Task<CallResult> CallResultAsync(string id);
}