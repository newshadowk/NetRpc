using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcServiceExtensions
    {
        #region Service

        public static IServiceCollection AddNetRpcService(this IServiceCollection services)
        {
            services.TryAddSingleton<MiddlewareBuilder>();
            services.TryAddSingleton<IGlobalServiceContextAccessor, GlobalServiceContextAccessor>();
            return services;
        }

        #endregion

        #region Middleware

        public static IServiceCollection AddNetRpcMiddleware(this IServiceCollection services, Action<MiddlewareOptions> configureOptions)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
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

        #endregion

        #region Contract

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

        internal static List<Instance> GetContractInstances(this IServiceProvider serviceProvider, ContractOptions options)
        {
            return options.Contracts.ConvertAll(i => new Instance(i, serviceProvider.GetRequiredService(i.ContractType)));
        }

        #endregion

        #region Client

        public static IServiceCollection AddNetRpcClientByOnceCallFactory<TOnceCallFactoryImplementation>(this IServiceCollection services,
            Action<NetRpcClientOption> configureOptions = null)
            where TOnceCallFactoryImplementation : class, IOnceCallFactory
        {
            services.TryAddSingleton<IOnceCallFactory, TOnceCallFactoryImplementation>();
            services.AddNetRpcClient(configureOptions);
            return services;
        }

        public static IServiceCollection AddNetRpcClientByClientConnectionFactory<TClientConnectionFactoryImplementation>(this IServiceCollection services, 
            Action<NetRpcClientOption> configureOptions = null)
            where TClientConnectionFactoryImplementation : class, IClientConnectionFactory
        {
            services.TryAddSingleton<IClientConnectionFactory, TClientConnectionFactoryImplementation>();
            services.AddNetRpcClient(configureOptions);
            return services;
        }

        public static IServiceCollection AddNetRpcClientContract<TService>(this IServiceCollection services)
        {
            services.TryAddSingleton<IClientProxy<TService>, ClientProxy<TService>>();
            services.TryAddSingleton(typeof(TService), p => p.GetService<IClientProxy<TService>>().Proxy);
            return services;
        }

        public static IServiceCollection AddNetRpcClientContract<TService>(this IServiceCollection services, string optionsName)
        {
            services.TryAddSingleton(typeof(IClientProxy<TService>), p => p.GetService<IClientProxyFactory>().CreateProxy<TService>(optionsName));
            services.TryAddSingleton(typeof(TService), p => p.GetService<IClientProxyFactory>().CreateProxy<TService>(optionsName).Proxy);
            return services;
        }

        private static IServiceCollection AddNetRpcClient(this IServiceCollection services, Action<NetRpcClientOption> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);

            services.TryAddSingleton<ClientMiddlewareBuilder>();
            services.TryAddSingleton<IClientProxyFactory, ClientProxyFactory>();
            return services;
        }

        #endregion
    }
}