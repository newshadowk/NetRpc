using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NetRpc
{
    public static class NetRpcServiceExtensions
    {
        public static IServiceCollection AddNetRpcService(this IServiceCollection services)
        {
            services.TryAddSingleton<MiddlewareBuilder>();
            return services;
        }

        public static IServiceCollection AddNetRpcContractSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            services.Configure<ContractOptions>(i => i.Contracts.Add(new Contract(serviceType, implementationType)));
            services.TryAddSingleton(serviceType, implementationType);
            return services;
        }

        public static IServiceCollection AddNetRpcContractSingleton(this IServiceCollection services, Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            services.Configure<ContractOptions>(i => i.Contracts.Add(new Contract(serviceType, null)));
            services.TryAddSingleton(serviceType, implementationFactory);
            return services;
        }

        public static IServiceCollection AddNetRpcContractSingleton(this IServiceCollection services, Type serviceType, object implementationInstance)
        {
            services.Configure<ContractOptions>(i => i.Contracts.Add(new Contract(serviceType, null)));
            services.AddSingleton(serviceType, implementationInstance);
            return services;
        }

        public static IServiceCollection AddNetRpcContractScoped(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            services.Configure<ContractOptions>(i => i.Contracts.Add(new Contract(serviceType, implementationType)));
            services.TryAddScoped(serviceType, implementationType);
            return services;
        }

        public static IServiceCollection AddNetRpcContractSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class
            where TImplementation : class, TService
        {
            services.AddNetRpcContractSingleton(typeof(TService), typeof(TImplementation));
            return services;
        }

        public static IServiceCollection AddNetRpcContractScoped<TService, TImplementation>(this IServiceCollection services) where TService : class
            where TImplementation : class, TService
        {
            services.AddNetRpcContractScoped(typeof(TService), typeof(TImplementation));
            return services;
        }

        public static IServiceCollection AddNetRpcClientByApiConvert<TOnceCallFactoryImplementation, TService>(this IServiceCollection services,
            Action<NetRpcClientOption> configureOptions)
            where TOnceCallFactoryImplementation : class, IOnceCallFactory
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.TryAddSingleton<IOnceCallFactory, TOnceCallFactoryImplementation>();
            services.TryAddSingleton<ClientProxy<TService>>();
            return services;
        }

        public static IServiceCollection AddNetRpcClient<TClientConnectionFactoryImplementation, TService>(this IServiceCollection services,
            Action<NetRpcClientOption> configureOptions)
            where TClientConnectionFactoryImplementation : class, IClientConnectionFactory
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.TryAddSingleton<IClientConnectionFactory, TClientConnectionFactoryImplementation>();
            services.TryAddSingleton<ClientProxy<TService>>();
            return services;
        }

        public static IServiceCollection AddNetRpcCallbackThrottling(this IServiceCollection services, int callbackThrottlingInterval)
        {
            services.Configure<MiddlewareOptions>(i => i.UseCallbackThrottling(callbackThrottlingInterval));
            return services;
        }

        public static IServiceCollection AddNetRpcStreamCallBack(this IServiceCollection services, int progressCount)
        {
            services.Configure<MiddlewareOptions>(i => i.UseStreamCallBack(progressCount));
            return services;
        }

        public static IServiceCollection AddNetRpcMiddleware(this IServiceCollection services, Action<MiddlewareOptions> configureOptions)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            return services;
        }

        public static List<Instance> GetContractInstances(this IServiceProvider serviceProvider, ContractOptions options)
        {
            return options.Contracts.ConvertAll(i => new Instance(i, serviceProvider.GetRequiredService(i.ContractType)));
        }
    }
}