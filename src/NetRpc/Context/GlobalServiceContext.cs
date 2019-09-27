using System.Threading;

namespace NetRpc
{
    public static class GlobalServiceContext
    {
        private static readonly AsyncLocal<ServiceContext> Local = new AsyncLocal<ServiceContext>();

        public static ServiceContext Context
        {
            get => Local.Value;
            set => Local.Value = value;
        }
    }
}