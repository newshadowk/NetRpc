using System.Threading.Tasks;

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

        public Task<IOnceCall> CreateAsync(int timeoutInterval)
        {
            return Task.FromResult<IOnceCall>(new OnceCall(new BufferClientOnceApiConvert(_factory.Create()), timeoutInterval));
        }
    }
}