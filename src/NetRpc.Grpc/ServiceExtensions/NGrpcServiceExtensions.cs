﻿using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection;

public static class NGrpcServiceExtensions
{
    public static IServiceCollection AddNGrpcService(this IServiceCollection services, Action<GrpcServiceOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.AddGrpc(configureOptions);
        else
            services.AddGrpc();
        services.AddNService();
        return services;
    }

    public static IApplicationBuilder UseNGrpc(this IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<MessageCallImpl>();
            endpoints.MapGet("/",
                async context =>
                {
                    await context.Response.WriteAsync(
                        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
        });
        return app;
    }

    public static IServiceCollection AddNGrpcGateway<TService>(this IServiceCollection services,
        Action<GrpcClientOptions>? configureGrpcClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null) where TService : class
    {
        services.AddNGrpcClient(configureGrpcClientOptions, configureClientOptions);
        services.Configure<NClientOptions>(i => i.ForwardAllHeaders = true);
        services.AddNClientContract<TService>();
        services.AddNServiceContract(typeof(TService),
            p => ((IClientProxy<TService>)p.GetService(typeof(IClientProxy<TService>))!).Proxy);
        return services;
    }

    public static IServiceCollection AddNGrpcClient(this IServiceCollection services,
        Action<GrpcClientOptions>? configureGrpcClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null)
    {
        if (configureGrpcClientOptions != null)
            services.Configure(configureGrpcClientOptions);
        services.AddLogging();
        services.AddNClientByClientConnectionFactory<GrpcClientConnectionFactory>(configureClientOptions);
        services.AddScoped<IClientProxyProvider, GrpcClientProxyProvider>();
        return services;
    }
}