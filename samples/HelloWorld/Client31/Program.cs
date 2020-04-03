using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Proxy.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:50001");
            var c = new MessageCall.MessageCallClient(channel);
            var api = c.DuplexStreamingServerMethod();
            var buffer = new StreamBuffer();
            buffer.Body = ByteString.CopyFromUtf8("1");
            await api.RequestStream.WriteAsync(buffer);
            await api.RequestStream.CompleteAsync();

            //var p = NetRpcManager.CreateClientProxy<IService>(new GrpcClientOptions
            //{
            //    Url = "http://localhost:50001"
            //});

            //await p.Proxy.Call("hello world.");

            Console.Read();
        }
    }
}