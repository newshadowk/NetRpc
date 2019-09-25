using System;

namespace NetRpc
{
    internal sealed class OnceCallFactory : IOnceCallFactory
    {
        private readonly IClientConnectionFactory _factory;
        private readonly ITraceIdAccessor _traceIdAccessor;

        public OnceCallFactory(IClientConnectionFactory factory, ITraceIdAccessor traceIdAccessor)
        {
            _factory = factory;
            _traceIdAccessor = traceIdAccessor;
        }

        public void Dispose()
        {
            _factory?.Dispose();
        }

        public IOnceCall<T> Create<T>(ContractInfo contract, int timeoutInterval)
        {
            return new OnceCall<T>(new BufferClientOnceApiConvert(_factory.Create()), timeoutInterval, _traceIdAccessor.TraceId);
        }
    }
}