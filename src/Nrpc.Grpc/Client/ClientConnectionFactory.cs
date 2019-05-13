using Grpc.Base;

namespace Nrpc.Grpc
{
    public class ClientConnectionFactory : IConnectionFactory
    {
        private readonly Client _client;

        public ClientConnectionFactory(string host, int port)
        {
            _client = new Client(host, port);
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