using Microsoft.OpenApi.Models;

namespace NetRpc.Http;

public interface INSwaggerProvider
{
    OpenApiDocument GetSwagger(string? apiRootPath, List<ContractInfo> contracts, string? key);
}