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
        if (context.PureArgs.Length == 0)
            return;

        var pis = context.ContractMethod.MethodInfo.GetParameters();
        for (int i = 0; i < context.PureArgs.Length; i++) 
            ValidateStart(pis[i], context.PureArgs[i]);

        await _next(context);
    }

    private static void ValidateStart(ParameterInfo pi, object? obj)
    {
        /*
        [V1]
        public class Obj5
        {
        }
        */
        var v = pi.ParameterType.GetCustomAttribute<ValidateValueAttribute>();
        v?.Validate(obj);

        /*
        public class Obj5
        {
            public Task T1([V1]string s1)
        }
        */
        v = pi.GetCustomAttribute<ValidateValueAttribute>();
        v?.Validate(obj);

        /*
        [Validate]
        public class Obj5
        {
        }
        */
        var v0 = pi.ParameterType.GetCustomAttribute<ValidateAttribute>();
        if (v0 == null)
            return;

        foreach (var i in pi.ParameterType.GetProperties())
            Validate(i, obj == null? null : i.GetValue(obj));
    }

    private static void Validate(PropertyInfo pi, object? piValue)
    {
        var v = pi.GetCustomAttribute<ValidateValueAttribute>();
        v?.Validate(piValue);

        var v0 = pi.PropertyType.GetCustomAttribute<ValidateAttribute>();
        if (v0 == null)
            return;

        foreach (var i in pi.PropertyType.GetProperties())
            Validate(i, piValue == null ? null : i.GetValue(piValue));
    }
}