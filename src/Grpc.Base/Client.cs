using System;
using System.Net;
using System.Net.Sockets;
using Grpc.Core;

namespace Grpc.Base
{
    public sealed class Client : IDisposable
    {
        private readonly string _ip;
        private readonly int _port;
        private Channel _channel;

        public MessageCall.MessageCallClient CallClient { get; private set; }

        public Client(string host, int port)
        {
            _ip = GetFirstIp(host);
            _port = port;
        }

        public void Connect()
        {
            _channel = new Channel(_ip, _port, ChannelCredentials.Insecure);
            CallClient = new MessageCall.MessageCallClient(_channel);
        }

        public void Dispose()
        {
            _channel?.ShutdownAsync().Wait();
        }

        public static string GetFirstIp(string host)
        {
            try
            {
                var ips = Dns.GetHostAddresses(host);
                foreach (var ip in ips)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}