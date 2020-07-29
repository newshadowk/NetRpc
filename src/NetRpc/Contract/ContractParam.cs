using System;

namespace NetRpc
{
    public class ContractParam
    {
        public Type ContractType { get; }

        public Type InstanceType { get; }

        public ContractParam(Type contractType, Type instanceType)
        {
            InstanceType = instanceType;
            ContractType = contractType;
        }
    }

    public sealed class ContractParam<TService, TImplementation> : ContractParam where TService : class
        where TImplementation : class, TService
    {
        public ContractParam() : base(typeof(TService), typeof(TImplementation))
        {
        }
    }
}