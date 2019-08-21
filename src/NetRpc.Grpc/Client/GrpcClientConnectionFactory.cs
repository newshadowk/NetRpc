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

        public IClientConnection Create()
        {
            return new GrpcClientConnection(_client);
        }
    }
}