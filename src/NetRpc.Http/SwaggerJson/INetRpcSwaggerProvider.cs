using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace NetRpc.Http
{
    public interface INetRpcSwaggerProvider
    {
        OpenApiDocument GetSwagger(string apiRootPath, IEnumerable<Type> instanceTypes);
    }
}