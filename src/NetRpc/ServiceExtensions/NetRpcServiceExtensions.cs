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

        public static IServiceCollection AddNetRpcServiceContract(this IServiceCollection services, Type implementationType,
            ContractLifeTime contractLifeTime = ContractLifeTime.Singleton)
        {
            services.Configure<ContractOptions>(i => i.Contracts.Add(implementationType));
            switch (contractLifeTime)
            {
                case ContractLifeTime.Singleton:
                    services.TryAddSingleton(implementationType);
                    break;
                case ContractLifeTime.Scoped:
                    services.TryAddScoped(implementationType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contractLifeTime), contractLifeTime, null);
            }

            return services;
        }

        public static IServiceCollection AddNetRpcServiceContract(this IServiceCollection services, IEnumerable<Type> implementationTypes,
            ContractLifeTime contractLifeTime = ContractLifeTime.Singleton)
        {
            foreach (var t in implementationTypes)
                services.AddNetRpcServiceContract(t, contractLifeTime);
            return services;
        }

        public static IServiceCollection AddNetRpcServiceContract<TImplementationType>(this IServiceCollection services,
            ContractLifeTime contractLifeTime = ContractLifeTime.Singleton) where TImplementationType : class
        {
            return services.AddNetRpcServiceContract(typeof(TImplementationType), contractLifeTime);
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

        public static IServiceCollection AddCallbackThrottling(this IServiceCollection services, int callbackThrottlingInterval)
        {
            services.Configure<MiddlewareOptions>(i =>i.UseCallbackThrottling(callbackThrottlingInterval));
            return services;
        }

        public static IServiceCollection AddNetRpcMiddleware(this IServiceCollection services, Action<MiddlewareOptions> configureOptions)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            return services;
        }

        public static object[] GetContractInstances(this IServiceProvider serviceProvider, ContractOptions options)
        {
            return options.Contracts.ConvertAll(serviceProvider.GetRequiredService).ToArray();
        }
    }
}