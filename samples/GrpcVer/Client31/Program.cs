using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Net.Client;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var c = GrpcChannel.ForAddress("https://localhost:5001");
            //var c = GrpcChannel.ForAddress("http://localhost:5000");
            var p = NetRpcManager.CreateClientProxy<IService>(c);
            await p.Proxy.Call("hello world.");

            //var channel = GrpcChannel.ForAddress("https://localhost:5001");
            //var client = new MessageCall.MessageCallClient(channel);
            //var m = client.DuplexStreamingServerMethod();
            //await m.RequestStream.WriteAsync(new StreamBuffer() {Body = ByteString.CopyFrom("123", Encoding.UTF8)});
            //Console.WriteLine("delay 1000");
            //await Task.Delay(1000);
            //Console.WriteLine("CompleteAsync");
            //await m.RequestStream.CompleteAsync();
            //Console.WriteLine("end");
            Console.Read();
        }
    }
}