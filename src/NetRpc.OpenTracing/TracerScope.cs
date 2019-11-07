using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public static class TracerScope
    {
        public static IScope BuildChild(string name)
        {
            return GlobalTracer.Instance.BuildSpan(name).AsChildOf(GlobalTracer.Instance.ActiveSpan).StartActive(true);
        }
    }
}