using Microsoft.OpenApi.Models;

namespace NetRpc.Http
{
    public interface INetRpcSwaggerProvider
    {
        OpenApiDocument GetSwagger(string apiRootPath, object[] instances);
    }
}