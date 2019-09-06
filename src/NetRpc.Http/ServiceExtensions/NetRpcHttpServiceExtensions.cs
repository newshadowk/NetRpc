using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NetRpc.Http.Client;
using Newtonsoft.Json.Serialization;

namespace NetRpc.Http
{
    public static class NetRpcHttpServiceExtensions
    {
        public static IServiceCollection AddNetRpcSwagger(this IServiceCollection services)
        {
            services.Configure<MvcJsonOptions>(c =>
            {
                if (c.SerializerSettings.ContractResolver is DefaultContractResolver r)
                    r.IgnoreSerializableInterface = true;
            });

            var paths = Helper.GetCommentsXmlPaths();
            services.AddSwaggerGen(i => paths.ForEach(path => i.IncludeXmlComments(path)));
            services.TryAddTransient<INetRpcSwaggerProvider, NetRpcSwaggerProvider>();
            return services;
        }

        public static IServiceCollection AddNetRpcHttp(this IServiceCollection services, Action<HttpServiceOptions> httpServiceConfigureOptions = null)
        {
            if (httpServiceConfigureOptions != null)
                services.Configure(httpServiceConfigureOptions);
            services.TryAddSingleton<RequestHandler>();
            return services;
        }

        public static IServiceCollection AddNetRpcHttpGateway<TService>(this IServiceCollection services,
            Action<HttpClientOptions> httpClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcHttpClient<TService>(httpClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((ClientProxy<TService>) p.GetService(typeof(ClientProxy<TService>))).Proxy);
            return services;
        }

        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app)
        {
            app.UseMiddleware<HttpNetRpcMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseNetRpcSwagger(this IApplicationBuilder app)
        {
            var opt = app.ApplicationServices.GetService<IOptions<HttpServiceOptions>>();
            var swaggerRootPath = opt.Value.ApiRootPath + "/swagger";
            app.UseMiddleware<SwaggerUiIndexMiddleware>();
            app.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(swaggerRootPath),
                FileProvider = new EmbeddedFileProvider(typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly, ConstValue.SwaggerUi3Base)
            });
            return app;
        }
    }
}