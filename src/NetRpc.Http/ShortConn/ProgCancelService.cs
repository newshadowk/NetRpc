using System.Threading.Tasks;
using NetRpc.Contract;

namespace NetRpc.Http.ShortConn;

[Tag("prog")]
[HttpRoute("prog")]
public interface IProgCancelService
{
    [HttpGet("prog/{id}")]
    Task<ContextData> CallProgressAsync(string id);

    [HttpGet("cancel/{id}")]
    Task CallCancel(string id);
}

public class ProgCancelService : IProgCancelService
{
    private readonly CacheHandler _cacheHandler;

    public ProgCancelService(CacheHandler cacheHandler)
    {
        _cacheHandler = cacheHandler;
    }

    public async Task<ContextData> CallProgressAsync(string id)
    {
        return await _cacheHandler.GetProgressAsync(id);
    }

    public Task CallCancel(string id)
    {
        _cacheHandler.Cancel(id);
        return Task.CompletedTask;
    }
}