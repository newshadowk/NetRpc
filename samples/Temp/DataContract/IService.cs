using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    [HttpGet]
    Task<string> CallAsync(A1 a1);
}

public class A1
{
    public string Year { get; set; }
}
