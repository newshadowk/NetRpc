using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("start");
            Channel c = new Channel("localhost", 5001, ChannelCredentials.Insecure);
           Console.WriteLine("end");
        }
    }
}