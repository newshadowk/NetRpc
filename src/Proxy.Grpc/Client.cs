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

        public Client(Channel channel)
        {
            _channel = channel;
        }

        public void Connect()
        {
            CallClient = new MessageCall.MessageCallClient(_channel);
        }

        public void Dispose()
        {
            _channel?.ShutdownAsync().Wait();
        }
    }
}