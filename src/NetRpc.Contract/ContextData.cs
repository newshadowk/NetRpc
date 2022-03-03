using System;

namespace NetRpc.Contract;

[Serializable]
public class ContextData
{
    public string? Prog { get; set; }

    public bool HasStream { get; set; }

    public string? StreamName { get; set; }

    public ContextStatus Status { get; set; }

    public int StatusCode { get; set; }

    public string? ErrCode { get; set; }

    public string? ErrMsg { get; set; }
}

public enum ContextStatus
{
    Prog,
    End,
    Err
}