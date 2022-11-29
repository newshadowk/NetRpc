namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Property)]
public abstract class ValueFilterAttribute : Attribute
{
    public abstract Task InvokeAsync(ValueContext context, IServiceProvider serviceProvider);
}

public class ValueContext
{
    public object? Value { get; set; }
}