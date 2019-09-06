using System;

namespace NetRpc
{
    public class Contract
    {
        public Type ContractType { get; set; }
        public Type InstanceType { get; set; }

        public Contract()
        {
        }

        public Contract(Type contractType, Type instanceType)
        {
            ContractType = contractType;
            InstanceType = instanceType;
        }
    }

    public sealed class Contract<TService, TImplementation> : Contract where TService : class
        where TImplementation : class, TService
    {
        public Contract()
        {
            ContractType = typeof(TService);
            InstanceType = typeof(TImplementation);
        }
    }
}