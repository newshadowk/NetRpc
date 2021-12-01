using System;
using System.Collections.Generic;

namespace NetRpc;

internal class ClientContractInfoCache
{
    private static readonly Dictionary<Type, ContractInfo> Dic = new();
    private static readonly object LockObj = new();

    public static ContractInfo GetOrAdd<T>()
    {
        return GetOrAdd(typeof(T));
    }

    public static ContractInfo GetOrAdd(Type type)
    {
        lock (LockObj)
        {
            if (Dic.TryGetValue(type, out var value))
                return value;
            var newInfo = new ContractInfo(type);
            Dic.Add(type, newInfo);
            return newInfo;
        }
    }
}