using System;
using System.Threading;

namespace NetRpc
{
    public static class GlobalServiceProvider
    {
        private static readonly AsyncLocal<IServiceProvider> Local = new AsyncLocal<IServiceProvider>();
        private static readonly AsyncLocal<IServiceProvider> LocalScope = new AsyncLocal<IServiceProvider>();

        public static IServiceProvider Provider
        {
            get => Local.Value;
            set => Local.Value = value;
        }

        public static IServiceProvider ScopeProvider
        {
            get => LocalScope.Value;
            set => LocalScope.Value = value;
        }
    }
}