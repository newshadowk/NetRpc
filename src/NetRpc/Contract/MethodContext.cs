namespace NetRpc;

public sealed class MethodContext
{
    public ContractMethod ContractMethod { get; set; }

    public InstanceMethod InstanceMethod { get; set; }

    public MethodContext(ContractMethod contractMethod, InstanceMethod instanceMethod)
    {
        ContractMethod = contractMethod;
        InstanceMethod = instanceMethod;
    }
}