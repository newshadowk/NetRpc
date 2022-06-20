using System;
using System.Collections.Generic;

namespace NetRpc;

[Serializable]
public sealed class OnceCallParam
{
    public ActionInfo Action { get; set; }

    public object?[] PureArgs { get; set; }

    public bool HasStream { get; set; }

    public long StreamLength { get; set; }

    public byte[]? PostStream { get; set; }

    public Dictionary<string, object?> Header { get; set; }

    public OnceCallParam(Dictionary<string, object?> header, ActionInfo action, bool hasStream, byte[]? postStream, long streamLength, object?[] pureArgs)
    {
        HasStream = hasStream;
        Action = action;
        PureArgs = pureArgs;
        Header = header;
        StreamLength = streamLength;
        PostStream = postStream;
    }

    public override string ToString()
    {
        return $"[OnceCallParam]\r\nMethod:{Action}\r\nHeader:{HeaderStr(Header)}\r\nPureArgs\r\n{ArgsStr(PureArgs)}";
    }

    private static string ArgsStr(IEnumerable<object?> list)
    {
        var s = "";
        int i = 0;
        foreach (var p in list)
            s += $"Param:{i++}\r\n{p.ToDtoJson()}\r\n";
        return s;
    }

    private static string HeaderStr(Dictionary<string, object?> header)
    {
        var s = "";
        foreach (var p in header)
            s += $"  {p.Key}:{p.Value}\r\n";
        return s;
    }
}