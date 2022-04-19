using System;
using System.Collections.Generic;

namespace NetRpc;

public interface IClientProxy<out TService> : IClientProxy where TService : class
{
    new TService Proxy { get; }
}

public interface IClientProxy : IDisposable
{
    Dictionary<string, object?> AdditionHeader { get; }
    Dictionary<string, object?> AdditionContextHeader { get; }
    object Proxy { get; }
}