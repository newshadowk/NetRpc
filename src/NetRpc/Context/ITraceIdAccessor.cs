using System;

namespace NetRpc
{
    public interface ITraceIdAccessor
    {
        string TraceId { get; set; }
    }
}