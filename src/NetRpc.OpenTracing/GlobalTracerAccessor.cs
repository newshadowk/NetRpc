using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public class GlobalTracerAccessor : IGlobalTracerAccessor
    {
        public ITracer GetGlobalTracer()
        {
            return GlobalTracer.Instance;
        }
    }

    public interface IGlobalTracerAccessor
    {
        ITracer GetGlobalTracer();
    }
}