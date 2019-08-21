using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NetRpc;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var webHost = NetRpcManager.CreateHost(
                5000,
                "/callback",
                true,
                new HttpServiceOptions { ApiRootPath = "/api" },
                null,
                typeof(ServiceAsync));
            await webHost.RunAsync();
        }
    }
}