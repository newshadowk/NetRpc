using System;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ
{
    public static class Helper
    {
        public static ConnectionFactory CreateConnectionFactory(this MQParam param)
        {
            return new ConnectionFactory
            {
                UserName = param.User,
                Password = param.Password,
                VirtualHost = param.VirtualHost,
                HostName = param.Host,
                Port = param.Port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };
        }
    }
}