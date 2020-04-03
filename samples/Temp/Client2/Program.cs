using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Model;
using Core.TranslateRawService.Contract;
using NetRpc.Grpc;
using Console = System.Console;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var c = NetRpcManager.CreateClientProxy<ITranslateRawService>(new GrpcClientOptions() {Url = "http://m.k8s.yx.com:34418"});
            var c = NetRpcManager.CreateClientProxy<ITranslateRawService>(new GrpcClientOptions() { Url = "http://localhost:8006" });
            
            var r = await c.Proxy.TranslateTextByEngineTypeAsync(new List<string>() { "hello" }, Guid.Empty, null, TranslateType.Chat,
                LanguageType.en, LanguageType.zs, null, null, default);

            Console.WriteLine("Hello World!");

        }
    }
}