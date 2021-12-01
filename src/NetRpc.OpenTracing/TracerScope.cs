using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing;

public static class TracerScope
{
    public static IScope BuildChild(string name)
    {
        var span = GlobalTracer.Instance.ActiveSpan;
        if (span != null)
        {
            return GlobalTracer.Instance.BuildSpan(name).AsChildOf(span).StartActive(true);
        }

        return GlobalTracer.Instance.BuildSpan(name).StartActive(true);
    }
}