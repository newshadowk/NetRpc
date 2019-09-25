using System;
using System.Threading;

namespace NetRpc
{
    public class TraceIdAccessor : ITraceIdAccessor
    {
        private static readonly AsyncLocal<string> Local = new AsyncLocal<string>();

        public string TraceId
        {
            get
            {
                if (Local.Value != null)
                    Local.Value = Guid.NewGuid().ToString("N");
                return Local.Value;
            }
            set => Local.Value = value;
        }
    }
}