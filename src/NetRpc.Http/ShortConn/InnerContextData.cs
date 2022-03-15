using System;

namespace NetRpc.Http;

[Serializable]
public class InnerContextData<T> where T : class
{
    public ContextData Data { get; } = new();

    public T? Result { get; set; }
}