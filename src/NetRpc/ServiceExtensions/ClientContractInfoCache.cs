using System;
using System.Collections.Concurrent;

namespace NetRpc
{
    public class ClientContractInfoCache
    {
        private static readonly ConcurrentDictionary<Type, ContractInfo> _dic = new ConcurrentDictionary<Type, ContractInfo>();

        public static ContractInfo GetOrAdd<T>()
        {
            var type = typeof(T);
            return _dic.GetOrAdd(type, t => new ContractInfo(t));
        }
    }
}