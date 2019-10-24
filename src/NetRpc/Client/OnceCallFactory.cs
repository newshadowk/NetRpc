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

        public IOnceCall Create(int timeoutInterval)
        {
            return new OnceCall(new BufferClientOnceApiConvert(_factory.Create()), timeoutInterval);
        }
    }
}