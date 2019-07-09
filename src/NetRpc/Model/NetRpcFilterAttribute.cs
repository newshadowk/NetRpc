using System;
using System.Threading.Tasks;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class NetRpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(ApiContext context);
    }
}