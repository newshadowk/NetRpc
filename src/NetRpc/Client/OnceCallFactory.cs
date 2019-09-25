using System;

namespace NetRpc
{
    internal sealed class OnceCallFactory : IOnceCallFactory
    {
        private readonly IClientConnectionFactory _factory;

        public OnceCallFactory(IClientConnectionFactory factory)
        {
            _factory = factory;
        }

        public void Dispose()
        {
            _factory?.Dispose();
        }

        public IOnceCall<T> Create<T>(ContractInfo contract, int timeoutInterval)
        {
            return new OnceCall<T>(new BufferClientOnceApiConvert(_factory.Create()), timeoutInterval);
        }
    }
}