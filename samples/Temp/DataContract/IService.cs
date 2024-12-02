using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    [HttpGet]
    Task<string> CallAsync(A1 a1);
}
//
// public interface IService2Async
// {
//     [HttpGet]
//     Task<string> CallAsync(B1 b1);
// }

public enum E1
{
    E1V,
    E2V, 
}

public class A1
{
    /// <summary>
    /// P1 sum 
    /// </summary>
    public E1 P1 { get; set; }

    /// <summary>
    /// P2 sum 
    /// </summary>
    public E1 P2 { get; set; }
}

public class B1
{
    public string Year { get; set; }
}