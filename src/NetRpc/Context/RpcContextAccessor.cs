using System.Threading;

namespace NetRpc
{
    public class RpcContextAccessor : IRpcContextAccessor
    {
        private static readonly AsyncLocal<RpcContext> Local = new AsyncLocal<RpcContext>();

        public RpcContext Context
        {
            get => Local.Value;
            set => Local.Value = value;
        }
    }
}