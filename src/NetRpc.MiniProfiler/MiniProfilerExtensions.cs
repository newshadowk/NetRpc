using System;
using Microsoft.AspNetCore.Builder;
using NetRpc.Http;
using NetRpc.MiniProfiler;
using StackExchange.Profiling;

namespace Microsoft.Extensions.DependencyInjection;

public static class MiniProfilerExtensions
{
    /// <param name="services"></param>
    /// <param name="configureOptions">https://miniprofiler.com/dotnet/AspDotNetCore</param>
    public static IServiceCollection AddNMiniProfiler(this IServiceCollection services, Action<MiniProfilerOptions>? configureOptions = null)
    {
        services.AddSingleton<IInjectSwaggerHtml, InjectSwaggerHtml>();

        configureOptions ??= options =>
        {
            // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

            // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
            options.RouteBasePath = "/profiler";

            // (Optional) Control storage
            // (default is 30 minutes in MemoryCacheStorage)
            // Note: MiniProfiler will not work if a SizeLimit is set on MemoryCache!
            //   See: https://github.com/MiniProfiler/dotnet/issues/501 for details
            //(options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

            // (Optional) Control which SQL formatter to use, InlineFormatter is the default
            //options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

            // (Optional) To control authorization, you can use the Func<HttpRequest, bool> options:
            // (default is everyone can access profilers)
            // options.ResultsAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
            // options.ResultsListAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
            // Or, there are async versions available:
            // options.ResultsAuthorizeAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfiler;
            // options.ResultsAuthorizeListAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfilerLists;

            // (Optional)  To control which requests are profiled, use the Func<HttpRequest, bool> option:
            // (default is everything should be profiled)
            // options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

            // (Optional) Profiles are stored under a user ID, function to get it:
            // (default is null, since above methods don't use it by default)
            // options.UserIdProvider = request => MyGetUserIdFunction(request);

            // (Optional) Swap out the entire profiler provider, if you want
            // (default handles async and works fine for almost all applications)
            // options.ProfilerProvider = new MyProfilerProvider();

            // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
            // (defaults to true, and connection opening/closing is tracked)
            //options.TrackConnectionOpenClose = true;

            // (Optional) Use something other than the "light" color scheme.
            // (defaults to "light")
            options.ColorScheme = ColorScheme.Auto;

            // The below are newer options, available in .NET Core 3.0 and above:

            // (Optional) You can disable MVC filter profiling
            // (defaults to true, and filters are profiled)
            //options.EnableMvcFilterProfiling = true;
            // ...or only save filters that take over a certain millisecond duration (including their children)
            // (defaults to null, and all filters are profiled)
            // options.MvcFilterMinimumSaveMs = 1.0m;

            // (Optional) You can disable MVC view profiling
            // (defaults to true, and views are profiled)
            //options.EnableMvcViewProfiling = true;
            // ...or only save views that take over a certain millisecond duration (including their children)
            // (defaults to null, and all views are profiled)
            // options.MvcViewMinimumSaveMs = 1.0m;

            // (Optional) listen to any errors that occur within MiniProfiler itself
            // options.OnInternalError = e => MyExceptionLogger(e);

            // (Optional - not recommended) You can enable a heavy debug mode with stacks and tooltips when using memory storage
            // It has a lot of overhead vs. normal profiling and should only be used with that in mind
            // (defaults to false, debug/heavy mode is off)
            //options.EnableDebugMode = true;
        };
        services.AddMiniProfiler(configureOptions).AddEntityFramework(); 

        return services;
    }

    public static void UseNMiniProfiler(this IApplicationBuilder app)
    {
        app.UseMiniProfiler();
    }
}