namespace NetRpc
{
    public sealed class Instance
    {
        public Contract Contract { get; }

        public object Value { get; }

        public Instance(Contract contract, object value)
        {
            Contract = contract;
            Value = value;
        }
    }
}