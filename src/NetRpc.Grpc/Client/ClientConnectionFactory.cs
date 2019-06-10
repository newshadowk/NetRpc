using Grpc.Base;
using Grpc.Core;

namespace NetRpc.Grpc
{
    public class ClientConnectionFactory : IConnectionFactory
    {
        private readonly Client _client;

        public ClientConnectionFactory(Channel channel)
        {
            _client = new Client(channel);
            _client.Connect();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public IConnection Create()
        {
            return new ClientConnection(_client);
        }
    }
}