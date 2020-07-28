using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace NetRpc.Http
{
    public interface INSwaggerProvider
    {
        OpenApiDocument GetSwagger(string? apiRootPath, List<Contract> contracts, string? key);
    }
}