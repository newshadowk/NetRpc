namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
public sealed class ClientNotRetryAttribute : Attribute
{
    public ClientNotRetryAttribute(Type exceptionType) : this(new[] { exceptionType })
    {
    }

    public ClientNotRetryAttribute(Type[] exceptionTypes)
    {
        ExceptionTypes = exceptionTypes;
    }

    public Type[] ExceptionTypes { get; }
}