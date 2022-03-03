using System;
using NetRpc.Contract;

namespace NetRpc.Http;

[Serializable]
public class InnerContextData
{
    public ContextData Data { get; } = new();

    public object? ResultWithOutStream { get; set; }
}