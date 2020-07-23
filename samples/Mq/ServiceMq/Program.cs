using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestHelper;

namespace ServiceMq
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Helper.GetMQOptions();
            var f = new ConnectionFactory
            {
                UserName = options.User,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                HostName = options.Host,
                Port = options.Port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true
            };

            //CreateChannel(f, "testQ");

            var s = new Service(f, "testQ", 1, NullLogger.Instance);
            s.ReceivedAsync += S_Received;
            s.Open();

            Console.Read();
        }

        public static void CreateChannel(ConnectionFactory f, string qName)
        {
            var c = f.CreateConnection();
            var _mainModel = c.CreateModel();
            _mainModel.QueueDeclare(qName, false, false, true, null);
            var consumer = new AsyncEventingBasicConsumer(_mainModel);
            _mainModel.BasicQos(0, 1, true);
            _mainModel.BasicConsume(qName, false, consumer);
            //consumer.Received += (s, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_connect, _mainModel, e, _logger)));
            consumer.Received += Consumer_Received;
        }

        private static async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {

        }

        private static async Task S_Received(object sender, EventArgsT<CallSession> e)
        {
            Console.WriteLine("S_Received");
            e.Value.ReceivedAsync += Value_Received;
            e.Value.Start();
        }

        private static async Task Value_Received(object sender, EventArgsT<ReadOnlyMemory<byte>> e)
        {
            var s = Encoding.UTF8.GetString(e.Value.Span);
            Console.WriteLine($"{s}");
            var callSession = (CallSession)sender;
            callSession.Dispose();
        }
    }
}
