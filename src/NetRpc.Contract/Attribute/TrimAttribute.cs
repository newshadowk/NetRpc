namespace NetRpc.Contract;

public class TrimAttribute : ValueFilterAttribute
{
    public override Task<object?> InvokeAsync(object? value, IServiceProvider serviceProvider)
    {
        if (value == null)
            return Task.FromResult<object?>(null);

        if (value is string s)
            return Task.FromResult<object?>(s.Trim());

        return Task.FromResult<object?>(value);
    }
}