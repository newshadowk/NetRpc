namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Property)]
public abstract class ValueFilterAttribute : Attribute
{
    public abstract Task<object?> InvokeAsync(object? value, IServiceProvider serviceProvider);
}
