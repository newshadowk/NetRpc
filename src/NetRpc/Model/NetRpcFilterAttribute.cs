using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public abstract class NetRpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(ApiContext context);
    }
}