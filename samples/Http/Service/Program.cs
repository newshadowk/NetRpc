using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using NetRpc;
using NetRpc.Http;
using NetRpcManager = NetRpc.Http.NetRpcManager;

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
                new Contract<IServiceAsync, ServiceAsync>());
            await webHost.RunAsync();
        }
    }
}