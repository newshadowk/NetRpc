using System;
using System.Text;
using System.Threading.Tasks;
using DataContract;
using Google.Protobuf;
using Grpc.Net.Client;
using NetRpc.Grpc;
using Proxy.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress("https://localhost:50001");
            var client = new MessageCall.MessageCallClient(channel);
            var api = client.DuplexStreamingServerMethod();
            await api.RequestStream.WriteAsync(new StreamBuffer() {Body = ByteString.CopyFrom("dsdf", Encoding.UTF8)});
            //var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions
            //{
            //    Url = "http://localhost:50001"
            //});

            //await p.Proxy.Call("hello world.");
            Console.Read();
        }
    }
}