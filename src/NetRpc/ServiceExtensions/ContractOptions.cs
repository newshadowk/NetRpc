using System;
using System.Collections.Generic;

namespace NetRpc
{
    public class ContractOptions
    {
        public List<Type> InstanceTypes { get; } = new List<Type>();
    }
}