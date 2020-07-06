using System;
using System.Threading.Tasks;
using NetRpc;

namespace ConsoleApp18
{
    class Program
    {
        static void Main(string[] args)
        {
            string s = "C1/Call1";
            
            var methodInfo = typeof(IService2Async).GetMethod("CallAsync");
            MethodRoute mr = new MethodRoute(typeof(IService2Async), methodInfo);
            Console.WriteLine(mr);

            var b = mr.MatchPath(s, "GET");
            Console.WriteLine($"=>{b}");
            Console.Read();
        }
    }

    [SwaggerTag("Ser")]
    [HttpRoute("C1")]
    //[HttpRoute("C2")]
    public interface IService2Async
    {
        [HttpRoute("Call1")]
        Task<string> CallAsync(string p1, int p2);

        [HttpGet(@"S/Call1/{P1}")]
        Task<string> Call1Async(string p1, int p2);

        [HttpGet]
        [HttpDelete]
        [HttpHead]
        [HttpPut]
        [HttpPatch]
        [HttpOptions]
        [HttpGet("S/Call2/{P1}/{I1}/Get")]
        [HttpPost("/S/Call2/{P1}/Post")]
        Task<string> Call2Async(CallObj obj);
    }

    [Serializable]
    public class CallObj
    {
        public string P1 { get; set; }

        public int P2 { get; set; }
    }

    [Serializable]
    public class InnerObj2
    {
        public string P3 { get; set; }

        public int I4 { get; set; }
    }
}
