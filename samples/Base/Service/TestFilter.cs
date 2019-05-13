using System;
using System.Threading.Tasks;
using Nrpc;

namespace Service
{
    public class TestFilter : NrpcFilterAttribute
    {
        public override Task InvokeAsync(ApiContext context)
        {
            Console.Write($"TestFilter.Execute(), context:{context}");
            return Task.CompletedTask;
        }
    }
}