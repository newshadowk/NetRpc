using System.IO;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public interface IServiceAsync
{
    [HttpGet]
    Task<string> CallAsync(A1 a1);

    // [HttpGet("a")]
    // Task<string> Call3Async([Trim]string s);
    //
    //
    // Task<string> Call2Async(Stream s);
}

public class A1
{
    /// <summary>
    /// 这是year说明
    /// </summary>
    public YearType Year { get; set; }

    /// <summary>
    /// 这是S1说明
    /// </summary>
    public string S1 { get; set; }
}

public enum YearType
{
    Y2012,
}