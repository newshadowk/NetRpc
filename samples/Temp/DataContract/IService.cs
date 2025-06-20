using System.Collections.Generic;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    Task<string> CallAsync(C1 a1);
}
//
// public interface IService2Async
// {
//     [HttpGet]
//     Task<string> CallAsync(B1 b1);
// }


public class C1
{
    /// <summary>
    /// Name des
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// C2 des
    /// </summary>
    public C2 C2 { get; set; } = new C2();
}

/// <summary>
/// C2 1.0
/// </summary>
public class C2
{
    public string Name { get; set; } = string.Empty;
}
