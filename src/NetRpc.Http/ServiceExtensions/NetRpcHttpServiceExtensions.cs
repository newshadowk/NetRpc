using System;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Http;
using NetRpc.Http.Client;
using Helper = NetRpc.Http.Helper;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcHttpServiceExtensions
    {
        public static IServiceCollection AddNetRpcSwagger(this IServiceCollection services)
        {
            var paths = Helper.GetCommentsXmlPaths();
            services.AddSwaggerGen(i =>
            {
#if !NETCOREAPP3_1
                i.DescribeAllEnumsAsStrings();
#endif
                paths.ForEach(path => { i.IncludeXmlComments(path); });
                i.SchemaFilter<ExampleSchemaFilter>();
            });

#if NETCOREAPP3_1
            services.AddControllers().AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
#endif
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
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            services.AddNetRpcHttpClient(httpClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.AddNetRpcClientContract<TService>(serviceLifetime);
            services.AddNetRpcServiceContract(typeof(TService),
                p => ((ClientProxy<TService>) p.GetService(typeof(ClientProxy<TService>))).Proxy,
                serviceLifetime);
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