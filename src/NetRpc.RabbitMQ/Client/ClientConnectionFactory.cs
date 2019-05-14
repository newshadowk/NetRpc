namespace NetRpc.RabbitMQ
{
    public class ClientConnectionFactory : IConnectionFactory
    {
        private readonly global::RabbitMQ.Client.IConnection _connection;
        private readonly MQParam _param;

        public ClientConnectionFactory(MQParam param)
        {
            _param = param;
            var factory = param.CreateConnectionFactory();
            _connection = factory.CreateConnection();
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        public IConnection Create()
        {
            return new ClientConnection(_connection, _param.RpcQueue);
        }
    }
}