using System;
using System.Threading.Tasks;
using NetRpc;

namespace Service
{
    public class TestFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Console.Write($"TestFilter.Execute(), context:{context}");
        }
    }
}