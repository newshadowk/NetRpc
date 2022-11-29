namespace NetRpc.Contract;

public class TrimAttribute : ValueFilterAttribute
{
    public override Task InvokeAsync(ValueContext context, IServiceProvider serviceProvider)
    {
        if (context.Value is string s) 
            context.Value = s.Trim();

        return Task.CompletedTask;
    }
}