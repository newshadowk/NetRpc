using OpenTracing;
using OpenTracing.Tag;
using System;

namespace NetRpc.OpenTracing;

public interface IErrorTagHandle
{
    void Handle(Exception e, ISpan span);
}

public abstract class BaseErrorTagHandle : IErrorTagHandle
{
    public virtual void Handle(Exception e, ISpan span)
    {
        SetError(span);
    }

    protected static void SetError(ISpan span)
    {
        Tags.Error.Set(span, true);
    }
}

internal class DefaultErrorTagHandle : BaseErrorTagHandle
{
}