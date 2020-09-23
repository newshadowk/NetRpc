using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Contract;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions
            {
                Host = "localhost",
                Port = 50001
            });

            try
            {
                await p.Proxy.Call("hello world.");
            }
            catch (FaultException e)
            {
                Console.WriteLine(e.Detail.StackTrace);
                throw;
            }
            catch (Exception e)
            {

            }
            

            Console.Read();
        }
    }
}