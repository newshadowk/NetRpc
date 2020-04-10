using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Net.Client;
using GrpcService1;
using NetRpc.Grpc;
using NetRpcManager = NetRpc.RabbitMQ.NetRpcManager;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            //var c = GrpcChannel.ForAddress("https://localhost:5001");
            //var c = GrpcChannel.ForAddress("http://localhost:5000");
            //var p = NetRpcManager.CreateClientProxy<IService>(c);
            //await p.Proxy.Call("hello world.");

            //var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions() { });
            //var channel = GrpcChannel.ForAddress("http://localhost:5000");
            //var client = new Greeter.GreeterClient(channel);
            //var r = client.SayHello(new HelloRequest() { Name = "n1" });
            //r = client.SayHello(new HelloRequest() { Name = "n2" });

            //var client = new MessageCall.MessageCallClient(channel);
            //var m = client.DuplexStreamingServerMethod();
            //await m.RequestStream.WriteAsync(new StreamBuffer() { Body = ByteString.CopyFrom("123", Encoding.UTF8) });
            //Console.WriteLine("delay 1000");
            //await Task.Delay(1000);
            //Console.WriteLine("CompleteAsync");
            //await m.RequestStream.CompleteAsync();
            //Console.WriteLine("end");

            var clientProxy = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>(new GrpcClientOptions()
            {
                Url = "http://localhost:5000"
            });

            //var clientProxy = NetRpcManager.CreateClientProxy<IService>(TestHelper.Helper.GetMQOptions());

            await clientProxy.Proxy.Call("1111");
            //await clientProxy.Proxy.Call("222");

            //using (var fs = File.OpenRead(@"d:\testfile\10mb.db"))
            //{
            //    var stream = await clientProxy.Proxy.Echo(fs);
            //    using (var fw = File.OpenWrite(@"d:\testfile\tgt.db"))
            //    {
            //        await stream.CopyToAsync(fw);
            //    }
            //}

            Console.Read();
        }
    }
}