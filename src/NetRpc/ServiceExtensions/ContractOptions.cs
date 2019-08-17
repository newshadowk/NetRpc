using System;
using System.Collections.Generic;

namespace NetRpc
{
    public class ContractOptions
    {
        public List<Type> Contracts { get; } = new List<Type>();
    }
}