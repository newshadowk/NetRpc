using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Http;
using NetRpc.Http.Client;
using Newtonsoft.Json.Serialization;
using Helper = NetRpc.Http.Helper;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcHttpServiceExtensions
    {
        public static IServiceCollection AddNetRpcSwagger(this IServiceCollection services)
        {

#if !NETCOREAPP3_0
            services.Configure<MvcJsonOptions>(c =>
            {
                if (c.SerializerSettings.ContractResolver is DefaultContractResolver r)
                    r.IgnoreSerializableInterface = true;
            });
#endif

            var paths = Helper.GetCommentsXmlPaths();
            services.AddSwaggerGen(i => paths.ForEach(path => i.IncludeXmlComments(path)));
            services.TryAddTransient<INetRpcSwaggerProvider, NetRpcSwaggerProvider>();
            return services;
        }

        public static IServiceCollection AddNetRpcHttpService(this IServiceCollection services, Action<HttpServiceOptions> httpServiceConfigureOptions = null)
        {
            if (httpServiceConfigureOptions != null)
                services.Configure(httpServiceConfigureOptions);
            services.TryAddSingleton(p => new RequestHandler(p, ChannelType.Http));
            services.AddNetRpcService();
            return services;
        }

        public static IServiceCollection AddNetRpcHttpGateway<TService>(this IServiceCollection services,
            Action<HttpClientOptions> httpClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcHttpClient(httpClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcClientContract<TService>();
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