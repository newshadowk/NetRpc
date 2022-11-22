using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;

namespace Microsoft.Extensions.DependencyInjection;

public static class NServiceExtensions
{
    #region Service

    public static IServiceCollection AddNService(this IServiceCollection services)
    {
        Helper.CheckBinSer();
        services.TryAddSingleton<BusyFlag>();
        services.TryAddSingleton<RequestHandler>();
        services.TryAddSingleton<MiddlewareBuilder>();
        services.TryAddSingleton<IActionExecutingContextAccessor, ActionExecutingContextAccessor>();
        return services;
    }

    public static IServiceCollection AddNServiceContract(this IServiceCollection services, Type serviceType, Type implementationType)
    {
        Helper.CheckContract(serviceType);
        services.Configure<ContractOptions>(i => i.Contracts.Add(new ContractInfo(serviceType, implementationType)));
        services.TryAddScoped(serviceType, implementationType);
        return services;
    }

    public static IServiceCollection AddNServiceContract(this IServiceCollection services, Type serviceType, object implementationInstance)
    {
        Helper.CheckContract(serviceType);

        services.Configure<ContractOptions>(i => i.Contracts.Add(new ContractInfo(serviceType, implementationInstance.GetType())));
        services.AddSingleton(serviceType, implementationInstance);
        return services;
    }

    public static IServiceCollection AddNServiceContract<TService, TImplementation>(this IServiceCollection services) where TService : class
        where TImplementation : class, TService
    {
        services.AddNServiceContract(typeof(TService), typeof(TImplementation));
        return services;
    }

    public static IServiceCollection AddNServiceContract(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory)
    {
        Helper.CheckContract(serviceType);
        services.Configure<ContractOptions>(i => i.Contracts.Add(new ContractInfo(serviceType)));
        services.TryAddScoped(serviceType, implementationFactory);
        return services;
    }

    public static List<Instance> GetContractInstances(this IServiceProvider serviceProvider, ContractOptions options)
    {
        return options.Contracts.ConvertAll(i =>
        {
            var requiredService = serviceProvider.GetRequiredService(i.Type);
            return new Instance(i, requiredService);
        });
    }

    #endregion

    #region Service Middleware

    public static IServiceCollection AddNMiddleware(this IServiceCollection services, Action<MiddlewareOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    public static IServiceCollection AddNCallbackThrottling(this IServiceCollection services, int callbackThrottlingInterval)
    {
        services.Configure<MiddlewareOptions>(i => i.UseCallbackThrottling(callbackThrottlingInterval));
        return services;
    }

    public static IServiceCollection AddNStreamCallBack(this IServiceCollection services, int progressCount)
    {
        services.Configure<MiddlewareOptions>(i => i.UseStreamCallBack(progressCount));
        return services;
    }

    #endregion

    #region Client

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddNClient(this IServiceCollection services, Action<NClientOptions>? configureOptions = null)
    {
        Helper.CheckBinSer();

        if (configureOptions != null)
            services.Configure(configureOptions);

        services.TryAddScoped<IClientProxyFactory, ClientProxyFactory>();
        services.TryAddSingleton<IActionExecutingContextAccessor, ActionExecutingContextAccessor>();

        return services;
    }

    public static IServiceCollection AddNClientContract<TService>(this IServiceCollection services) where TService : class
    {
        Helper.CheckContract(typeof(TService));

        ClientContractInfoCache.GetOrAdd<TService>();

        services.TryAddScoped<IClientProxy<TService>, ClientProxy<TService>>();
        services.TryAddScoped(typeof(TService), p => p.GetService<IClientProxy<TService>>()!.Proxy);

        return services;
    }

    public static IServiceCollection AddNClientContract<TService>(this IServiceCollection services, string optionsName) where TService : class
    {
        Helper.CheckContract(typeof(TService));

        ClientContractInfoCache.GetOrAdd<TService>();

        services.TryAddScoped(typeof(IClientProxy<TService>), p => p.GetService<IClientProxyFactory>()!.CreateProxy<TService>(optionsName)!);
        services.TryAddScoped(typeof(TService), p => p.GetService<IClientProxyFactory>()!.CreateProxy<TService>(optionsName)!.Proxy);

        return services;
    }

    public static IServiceCollection AddNClientByOnceCallFactory<TOnceCallFactoryImplementation>(this IServiceCollection services,
        Action<NClientOptions>? configureOptions = null)
        where TOnceCallFactoryImplementation : class, IOnceCallFactory
    {
        services.TryAddScoped<IOnceCallFactory, TOnceCallFactoryImplementation>();
        services.AddNClient(configureOptions);
        return services;
    }

    public static IServiceCollection AddNClientByClientConnectionFactory<TClientConnectionFactoryImplementation>(this IServiceCollection services,
        Action<NClientOptions>? configureOptions = null)
        where TClientConnectionFactoryImplementation : class, IClientConnectionFactory
    {
        services.TryAddScoped<IClientConnectionFactory, TClientConnectionFactoryImplementation>();
        services.AddNClient(configureOptions);
        return services;
    }

    #endregion
}