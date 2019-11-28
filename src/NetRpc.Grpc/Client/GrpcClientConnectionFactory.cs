using Grpc.Base;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientConnectionFactory : IClientConnectionFactory
    {
        private Client _client;

        public GrpcClientConnectionFactory(IOptionsMonitor<GrpcClientOptions> options)
        {
            Reset(options.CurrentValue.Channel);
            options.OnChange(i => Reset(i.Channel));
        }

        public void Reset(Channel channel)
        {
            Dispose();
            _client = new Client(channel);
            _client.Connect();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

#if NETSTANDARD2_1
        public System.Threading.Tasks.ValueTask DisposeAsync()
        {
            return _client.DisposeAsync();
        }
#endif

        public IClientConnection Create()
        {
            return new GrpcClientConnection(_client);
        }
    }
}