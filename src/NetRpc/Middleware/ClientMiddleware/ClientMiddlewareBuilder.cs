using System.Linq.Expressions;
using System.Reflection;

namespace NetRpc;

public delegate Task ClientRequestDelegate(ClientActionExecutingContext context);

public class ClientMiddlewareBuilder
{
    internal const string InvokeMethodName = "Invoke";
    internal const string InvokeAsyncMethodName = "InvokeAsync";

    private static readonly MethodInfo GetServiceInfo =
        typeof(ClientMiddlewareBuilder).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    private ClientRequestDelegate? _requestDelegate;
    private readonly object _lockRequestDelegate = new();
    private readonly IList<Func<ClientRequestDelegate, ClientRequestDelegate>> _components = new List<Func<ClientRequestDelegate, ClientRequestDelegate>>();

    public ClientMiddlewareBuilder(ClientMiddlewareOptions options, IServiceProvider serviceProvider)
    {
        options.Items.ForEach(i => _components.Add(CreateMiddleware(serviceProvider, i.Type, i.args)));
        _components.Add(CreateMiddleware(serviceProvider, typeof(ClientMethodInvokeMiddleware), new object[] { }));
    }

    public async Task<object?> InvokeAsync(ClientActionExecutingContext context)
    {
        var requestDelegate = GetRequestDelegate();
        await requestDelegate.Invoke(context);
        return context.Result;
    }

    private ClientRequestDelegate GetRequestDelegate()
    {
        if (_requestDelegate == null)
            lock (_lockRequestDelegate)
                if (_requestDelegate == null)
                    _requestDelegate = Build();
        return _requestDelegate;
    }

    private ClientRequestDelegate Build()
    {
        ClientRequestDelegate app = _ => Task.CompletedTask;
        foreach (var component in _components.Reverse())
            app = component(app);
        return app;
    }

    private static Func<ClientRequestDelegate, ClientRequestDelegate> CreateMiddleware(IServiceProvider serviceProvider, Type middleware, object[] args)
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
            if (parameters.Length == 0 || parameters[0].ParameterType != typeof(ClientActionExecutingContext))
                throw new InvalidOperationException($"UseMiddlewareNoParameters, {InvokeMethodName}, {InvokeAsyncMethodName}");

            var ctorArgs = new object[args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(args, 0, ctorArgs, 1, args.Length);

            var instance = ActivatorUtilities.CreateInstance(serviceProvider, middleware, ctorArgs);

            if (parameters.Length == 1)
                return (ClientRequestDelegate)methodInfo.CreateDelegate(typeof(ClientRequestDelegate), instance);

            var factory = Compile<object>(methodInfo, parameters);
            return context => factory(instance, context, context.ServiceProvider);
        };
    }

    private static Func<T, ClientActionExecutingContext, IServiceProvider, Task> Compile<T>(MethodInfo methodInfo, ParameterInfo[] parameters)
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

        var middlewareContextArg = Expression.Parameter(typeof(ClientActionExecutingContext), "apiContext");
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

        var lambda = Expression.Lambda<Func<T, ClientActionExecutingContext, IServiceProvider, Task>>(body, instanceArg, middlewareContextArg, providerArg);

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