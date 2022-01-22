using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract;

public interface IServiceAsync
{
    //Task<string> CallAsync(string s);

    Task<string> Call2(P p, Stream stream, Func<double, Task> cb, CancellationToken token);
}

public class P
{
    public string StreamName { get; set; }
    public string P1 { get; set; }
}