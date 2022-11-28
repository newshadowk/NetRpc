using System.Reflection;
using NetRpc.Contract;

namespace NetRpc;

public class ValidateMiddleware
{
    private readonly RequestDelegate _next;

    public ValidateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ActionExecutingContext context)
    {
        foreach (var p in context.PureArgs)
            ValidateStart(p);
        await _next(context);
    }

    private static void ValidateStart(object? obj)
    {
        if (obj == null)
            return;

        var type = obj.GetType();
        var v = type.GetCustomAttribute<ValidateValueAttribute>();
        v?.Validate(obj);

        var v0 = type.GetCustomAttribute<ValidateAttribute>();
        if (v0 == null)
            return;

        foreach (var pi in type.GetProperties())
            Validate(pi, pi.GetValue(obj));
    }

    private static void Validate(PropertyInfo pi, object? piValue)
    {
        var v = pi.GetCustomAttribute<ValidateValueAttribute>();
        v?.Validate(piValue);

        if (piValue == null)
            return;

        var v0 = pi.PropertyType.GetCustomAttribute<ValidateAttribute>();
        if (v0 == null)
            return;

        foreach (var i in pi.PropertyType.GetProperties())
            Validate(i, i.GetValue(piValue));
    }
}