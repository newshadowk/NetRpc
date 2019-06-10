using System.Collections.Generic;
using Grpc.Core;

namespace NetRpc.Grpc
{
    public static class NetRpcManager
    {
        public static ServiceProxy CreateServiceProxy(List<ServerPort> ports, params object[] instances)
        {
            return new ServiceProxy(ports, instances);
        }

        public static ServiceProxy CreateServiceProxy(ServerPort port, params object[] instances)
        {
            return CreateServiceProxy(new List<ServerPort> {port}, instances);
        }

        public static ServiceProxy CreateServiceProxy(string host, int port, string publicKey, string privateKey, params object[] instances)
        {
            var keyPair = new KeyCertificatePair(publicKey, privateKey);
            var sslCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyPair});
            ServerPort serverPort = new ServerPort(host, port, sslCredentials);
            return CreateServiceProxy(new List<ServerPort> { serverPort }, instances);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(Channel channel, bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var factory = new ClientConnectionFactory(channel);
            return new GrpcClientProxy<TService>(factory, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(ClientConnectionFactory factory, bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new GrpcClientProxy<TService>(factory, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, string publicKey, string sslTargetName, bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var ssl = new SslCredentials(publicKey);
            var options = new List<ChannelOption>();
            options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride,sslTargetName));
            Channel channel = new Channel(host, port, ssl, options);
            var factory = new ClientConnectionFactory(channel);
            return new GrpcClientProxy<TService>(factory, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }
    }
}