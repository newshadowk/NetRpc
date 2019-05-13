using System;
using System.Threading.Tasks;

namespace Nrpc
{
    public abstract class NrpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(ApiContext context);
    }
}