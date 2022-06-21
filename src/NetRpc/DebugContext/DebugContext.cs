using System;
using System.Text;
using System.Threading;

namespace NetRpc;

public class DebugContext
{
    private readonly string _id = Guid.NewGuid().ToString()[32..];

    private readonly StringBuilder _sb = new("[Debug Context]\r\n");

    public void Info(string s)
    {
        var ss = $"---> {_id} {T()} {s}";
        _sb.AppendLine(ss);
        _sb.AppendLine("\r\n");
        //Console.WriteLine(ss);
    }

    private static string T()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }

    public override string ToString()
    {
        return _sb.ToString();
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
