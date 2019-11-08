using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NetRpc.RabbitMQ.NetRpcManager.CreateClientProxy<IService>(TestHelper.Helper.GetMQOptions());
            //var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            await p.Proxy.Call("msg");

            //using (var s = File.OpenRead(@"d:\7\3.rar"))
            //{
            //    var stream = await p.Proxy.Echo(s);
            //    MemoryStream ms = new MemoryStream();
            //    stream.CopyTo(ms);
            //}

            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}