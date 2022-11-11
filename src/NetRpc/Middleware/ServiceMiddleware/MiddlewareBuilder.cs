using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace NetRpc;

public delegate Task RequestDelegate(ActionExecutingContext context);

public class MiddlewareBuilder
{
    internal const string InvokeMethodName = "Invoke";
    internal const string InvokeAsyncMethodName = "InvokeAsync";

    private static readonly MethodInfo GetServiceInfo =
        typeof(MiddlewareBuilder).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    private RequestDelegate? _requestDelegate;
    private readonly object _lockRequestDelegate = new();
    private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

    public MiddlewareBuilder(IOptions<MiddlewareOptions> options, IServiceProvider serviceProvider)
    {
        _components.Add(CreateMiddleware(serviceProvider, typeof(IgnoreMiddleware), new object[] { }));
        options.Value.GetItems().ForEach(i => _components.Add(CreateMiddleware(serviceProvider, i.Type, i.args)));
        _components.Add(CreateMiddleware(serviceProvider, typeof(MethodInvokeMiddleware), new object[] { }));
    }

    public async Task<object?> InvokeAsync(ActionExecutingContext context)
    {
        var requestDelegate = GetRequestDelegate();
        await requestDelegate.Invoke(context);
        return context.Result;
    }

    private RequestDelegate GetRequestDelegate()
    {
        if (_requestDelegate == null)
            lock (_lockRequestDelegate)
                if (_requestDelegate == null)
                    _requestDelegate = Build();
        return _requestDelegate;
    }

    private RequestDelegate Build()
    {
        RequestDelegate requestDelegate = _ => Task.CompletedTask;
        foreach (var component in _components.Reverse())
            requestDelegate = component(requestDelegate);
        return requestDelegate;
    }

    private static Func<RequestDelegate, RequestDelegate> CreateMiddleware(IServiceProvider serviceProvider, Type middleware, object[] args)
    {
        return next =>
        {
            var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var invokeMethods = methods.Where(m =>
                string.Equals(m.Name, InvokeMethodName, StringComparison.Ordinal)
                || string.Equals(m.Name, InvokeAsyncMethodName, StringComparison.Ordinal)
            ).ToArray();

            if (invokeMethods.Length > 1)
                throw new InvalidOperationException($"UseMiddleMultipleInvokes, {InvokeMethodName}, {InvokeAsyncMethodName}");

            if (invokeMethods.Length == 0)
                throw new InvalidOperationException($"UseMiddlewareNoInvokeMethod, {InvokeMethodName}, {InvokeAsyncMethodName}");

            var methodInfo = invokeMethods[0];
            var parameters = methodInfo.GetParameters();
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(ActionExecutingContext))
                throw new InvalidOperationException($"UseMiddlewareNoParameters, {InvokeMethodName}, {InvokeAsyncMethodName}");

            var ctorArgs = new object[args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(args, 0, ctorArgs, 1, args.Length);

            var instance = ActivatorUtilities.CreateInstance(serviceProvider, middleware, ctorArgs);

            if (parameters.Length == 1)
                return (RequestDelegate)methodInfo.CreateDelegate(typeof(RequestDelegate), instance);

            var factory = Compile<object>(methodInfo, parameters);
            return context => factory(instance, context, context.ServiceProvider);
        };
    }

    private static Func<T, ActionExecutingContext, IServiceProvider, Task> Compile<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
    {
        // If we call something like
        //
        // public class Middleware
        // {
        //    public Task Invoke(ApiContext context, ILoggerFactory loggerFactory)
        //    {
        //
        //    }
        // }
        //

        // We'll end up with something like this:
        //   Generic version:
        //
        //   Task Invoke(Middleware instance, MiddlewareContext context, IServiceProvider provider)
        //   {
        //      return instance.Invoke(context, (ILoggerFactory)MiddlewareBuilder.GetService(provider, typeof(ILoggerFactory));
        //   }

        //   Non generic version:
        //
        //   Task Invoke(object instance, MiddlewareContext context, IServiceProvider provider)
        //   {
        //      return ((Middleware)instance).Invoke(context, (ILoggerFactory)MiddlewareBuilder.GetService(provider, typeof(ILoggerFactory));
        //   }

        var middleware = typeof(T);

        var middlewareContextArg = Expression.Parameter(typeof(ActionExecutingContext), "apiContext");
        var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var instanceArg = Expression.Parameter(middleware, "middleware");

        var methodArguments = new Expression[parameters.Length];
        methodArguments[0] = middlewareContextArg;
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
                throw new NotSupportedException($"InvokeDoesNotSupportRefOrOutParams, {InvokeMethodName}");

            var parameterTypeExpression = new Expression[]
            {
                providerArg,
                Expression.Constant(parameterType, typeof(Type)),
                Expression.Constant(methodInfo.DeclaringType, typeof(Type))
            };

            var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
            methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
        }

        Expression middlewareInstanceArg = instanceArg;
        if (methodInfo.DeclaringType != typeof(T))
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodInfo.DeclaringType!);
        }

        var body = Expression.Call(middlewareInstanceArg, methodInfo, methodArguments);

        var lambda = Expression.Lambda<Func<T, ActionExecutingContext, IServiceProvider, Task>>(body, instanceArg, middlewareContextArg, providerArg);

        return lambda.Compile();
    }

    private static object GetService(IServiceProvider sp, Type type, Type middleware)
    {
        var service = sp.GetService(type);
        if (service == null)
        {
            throw new InvalidOperationException($"InvokeMiddlewareNoService, {type}, {middleware}");
        }

        return service;
    }
}