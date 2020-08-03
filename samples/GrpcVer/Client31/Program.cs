using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Net.Client;
using GrpcService1;
using NetRpc.Grpc;
using NManager = NetRpc.RabbitMQ.NManager;

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
            //var p = NManager.CreateClientProxy<IService>(c);
            //await p.Proxy.Call("hello world.");

            //var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions() { });
            var channel = GrpcChannel.ForAddress("http://localhost:5000");
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




            //var clientProxy = NetRpc.Grpc.NManager.CreateClientProxy<IService>(new GrpcClientOptions()
            //{
            //    Url = "http://localhost:5000"
            //});

            //await clientProxy.Proxy.Call("1111");

            Console.Read();
        }
    }
}