using System;
using System.Threading.Tasks;
using NetRpc;

namespace Service
{
    public class TestFilter : NetRpcFilterAttribute
    {
        public override Task InvokeAsync(ServiceContext context)
        {
            Console.Write($"TestFilter.Execute(), context:{context}");
            return Task.CompletedTask;
        }
    }
}