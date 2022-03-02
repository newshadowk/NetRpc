```c#
//RabbitMQ client
var services = new ServiceCollection();
services.AddNClientContract<IServiceAsync>();
services.AddNClientContract<IService>();
services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
var sp = services.BuildServiceProvider();
var clientProxy = sp.GetService<IClientProxy<IService>>();
clientProxy.Connected += (_, _) => Console.WriteLine("[event] Connected");
clientProxy.DisConnected += (_, _) => Console.WriteLine("[event] DisConnected");
clientProxy.ExceptionInvoked += (_, _) => Console.WriteLine("[event] ExceptionInvoked");

//Heartbeat
clientProxy.HeartbeatAsync += (s, e) =>
{
    Console.WriteLine("[event] Heartbeat");
    ((IService)((IClientProxy)s).Proxy).Hearbeat();
    return Task.CompletedTask;
};
clientProxy.StartHeartbeat(true);

var proxy = clientProxy.Proxy;
var proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
```
```c#
//Grpc client
var services = new ServiceCollection();
services.AddNClientContract<IServiceAsync>();
services.AddNClientContract<IService>();
services.AddNGrpcClient(o => o.Url = "http://localhost:50001");
var sp = services.BuildServiceProvider();
var clientProxy = sp.GetService<IClientProxy<IService>>();
var proxy = clientProxy.Proxy;
var proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
```
```c#
//Http client
var services = new ServiceCollection();
services.AddNClientContract<IService>();
services.AddNClientContract<IServiceAsync>();
services.AddNHttpClient(o =>
{
    o.ApiUrl = "http://localhost:50002/api";
});
var sp = services.BuildServiceProvider();
var clientProxy = sp.GetService<IClientProxy<IService>>();
var proxy = clientProxy.Proxy;
var proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
```

```c#
//rabbitMQ service
var mpHost = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
        services.AddNServiceContract<IServiceAsync, ServiceAsync>();
        services.AddNServiceContract<IService, Service>();
    })
    .Build();
await mpHost.RunAsync();
```
```c#
//grpc service
var grpcHost = Host.CreateDefaultBuilder()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel((_, options) =>
            {
                options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddNGrpcService();
                services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                services.AddNServiceContract<IService, Service>();
            }).Configure(app => { app.UseNGrpc(); });
    }).Build();
await grpcHost.RunAsync();
```
```c#
//http service
var httpHost = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureKestrel((_, options) =>
            {
                options.Limits.MaxRequestBodySize = 10737418240; //10G
                options.ListenAnyIP(50002);
            })
            .ConfigureServices(services =>
            {
                services.AddCors();
                services.AddSignalR();
                services.AddNHttpService();
                services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                services.AddNServiceContract<IService, Service>();
            })
            .Configure(app =>
            {
                app.UseCors(set =>
                {
                    set.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });

                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapHub<CallbackHub>("/callback"); });
                app.UseNHttp();
            });
    }).Build();
await httpHost.RunAsync();
```
