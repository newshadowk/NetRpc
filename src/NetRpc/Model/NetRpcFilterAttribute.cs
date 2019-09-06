using System;
using System.Threading.Tasks;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class NetRpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(RpcContext context);
    }

    public class StreamCallBackFilterAttribute : NetRpcFilterAttribute
    {
        private readonly int _progressCount;

        public StreamCallBackFilterAttribute(int progressCount)
        {
            _progressCount = progressCount;
        }

        public override Task InvokeAsync(RpcContext context)
        {
            var rate = (double) _progressCount / 100;
            var bbs = (BufferBlockStream) context.Stream;
            var totalCount = bbs.Length;

            bbs.Progress += (s, e) =>
            {
                var p = (double) e / totalCount;
                var dd = p * rate * 100;
                context.Callback((int) dd);
            };

            return Task.CompletedTask;
        }
    }
}