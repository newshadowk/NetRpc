using System;
using System.Runtime.Serialization;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetRpc.Http;
using Newtonsoft.Json.Serialization;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //there is two way to open a service:

            //1 use NetRpcManager create service.
            //var p = NetRpcManager.CreateServiceProxy(5000, "/callback", new ServiceAsync());
            //p.Open();

            //2 use 'IApplicationBuilder.UseNetRpcHttp' to inject to your own exist service(like mvc web site).

            //CustomObj o = new CustomObj();
            //o.Name = NameEnum.John;
            //var s = JsonConvert.SerializeObject(o);

            //var deserializeObject = JsonConvert.DeserializeObject<CustomObj>(s);

            //var dd = typeof(C1).GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty);
            //var dd = typeof(C1).GetProperties();

            //CustomObj o = new CustomObj();
            //SetDefaultValue(o);
            //var serializeObject = JsonConvert.SerializeObject(o);
            //var serializeObject2 = JsonConvert.SerializeObject(o, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling. });
            //var serializeObject3 = JsonConvert.SerializeObject(o, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Include });

            //var json = jsonSchema.

            var r = new DefaultContractResolver();
            //r.IgnoreSerializableAttribute = true;
            r.IgnoreSerializableInterface = true;
            var resolveContract = r.ResolveContract(typeof(Exception));

            //JsonObjectContract resolveContract2 = (JsonObjectContract)r.ResolveContract(typeof(Exception));



            var webHost = GetWebHost();
            webHost.Run();

            Console.ReadLine();
        }

        static IWebHost GetWebHost()
        {
            var origins = "_myAllowSpecificOrigins";
            var swaggerRootPath = "/s";

            return WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5000); })
                .ConfigureServices(services =>
                {
                    services.AddCors(op =>
                    {
                        op.AddPolicy(origins, set =>
                        {
                            set.SetIsOriginAllowed(origin => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                    });
                    services.AddSignalR();
                    services.AddDirectoryBrowser();
                    services.AddNetRpcSwagger();
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcHttp("/api", true, new ServiceAsync());
                })
                .Build();
        }
    }

    struct MyStruct
    {
        private int d;
        private C1 c1;
    }

    [Serializable]
    public class C3 : Exception
    {
   
    }

    [Serializable]
    public class C2 
    {
        
    }

    public class C1
    {
        public string P1 { get; set; }

        public string P2 { get; }
    }
}