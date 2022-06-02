using System;
using System.Threading;

namespace NetRpc;

public class DebugContext
{
    private readonly string _id = Guid.NewGuid().ToString()[32..];

    private string _s = "[Debug Context]\r\n";

    public void Info(string s)
    {
        var ss = $"---> {_id} {T()} {s}";
        _s += ss + "\r\n";
        //Console.WriteLine(ss);
    }

    private static string T()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }

    public override string ToString()
    {
        return $"{_s}";
    }
}

public static class GlobalDebugContext
{
    private static readonly AsyncLocal<DebugContext> Local = new();

    public static DebugContext Context
    {
        get
        {
            if (Local.Value == null)
                Local.Value = new ();
            return Local.Value;
        }
    }
}
