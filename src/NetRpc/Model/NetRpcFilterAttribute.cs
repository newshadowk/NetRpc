using System;
using System.Threading.Tasks;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class NetRpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(ServiceContext context);
    }

    public class StreamCallBackFilterAttribute : NetRpcFilterAttribute
    {
        private readonly int _progressCount;

        public StreamCallBackFilterAttribute(int progressCount)
        {
            _progressCount = progressCount;
        }

        public override Task InvokeAsync(ServiceContext context)
        {
            Helper.ConvertStreamProgress(context, _progressCount);
            return Task.CompletedTask;
        }
    }
}