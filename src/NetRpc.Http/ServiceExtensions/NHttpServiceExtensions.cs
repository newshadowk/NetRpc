﻿using System;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Http;
using NetRpc.Http.Abstract;
using NetRpc.Http.Client;
using Helper = NetRpc.Http.Helper;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NHttpServiceExtensions
    {
        public static IServiceCollection AddNSwagger(this IServiceCollection services, Action<SwaggerOptions>? configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);

            var paths = Helper.GetCommentsXmlPaths();

            services.Configure<DocXmlOptions>(i => i.Paths.AddRange(paths));

            services.AddSwaggerGen(i =>
            {
#if !NETCOREAPP3_1
#pragma warning disable 618
                i.DescribeAllEnumsAsStrings();
#pragma warning restore 618
#endif
                paths.ForEach(path => { i.IncludeXmlComments(path); });
                i.SchemaFilter<ExampleSchemaFilter>();
            });

#if NETCOREAPP3_1
            services.AddControllers().AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
#endif

            services.TryAddTransient<INSwaggerProvider, NSwaggerProvider>();
            services.TryAddSingleton<SwaggerKeyRoles>();
            services.TryAddSingleton<IInjectSwaggerHtml, NullInjectSwaggerHtml>();
            return services;
        }

        public static IServiceCollection AddNHttpService(this IServiceCollection services, Action<HttpServiceOptions>? httpServiceConfigureOptions = null)
        {
            if (httpServiceConfigureOptions != null)
                services.Configure(httpServiceConfigureOptions);

            //HttpObjProcessor
            services.TryAddSingleton<HttpObjProcessorManager>();
            services.AddSingleton<IHttpObjProcessor, FormDataHttpObjProcessor>();
            services.AddSingleton<IHttpObjProcessor, JsonHttpObjProcessor>();
            services.AddSingleton<IHttpObjProcessor, FormUrlEncodedObjProcessor>();

            services.TryAddTransient<PathProcessor>();
            services.AddNService();
            return services;
        }

        public static IServiceCollection AddNHttpGateway<TService>(this IServiceCollection services,
            Action<HttpClientOptions>? httpClientConfigureOptions = null,
            Action<NClientOption>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) where TService : class
        {
            services.AddNHttpClient(httpClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.AddNClientContract<TService>(serviceLifetime);
            services.AddNServiceContract(typeof(TService),
                p => ((ClientProxy<TService>)p.GetService(typeof(ClientProxy<TService>))).Proxy,
                serviceLifetime);
            return services;
        }

        public static IApplicationBuilder UseNHttp(this IApplicationBuilder app)
        {
            app.UseMiddleware<NHttpMiddleware>();
            return app;
        }

        public static IApplicationBuilder UseNSwagger(this IApplicationBuilder app)
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