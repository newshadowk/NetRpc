using System.Collections.Generic;
using Grpc.Core;

namespace NetRpc.Grpc
{
    public sealed class NGrpcServiceOptions
    {
        public List<ServerPort> Ports { get; set; } = new List<ServerPort>();

        public void AddPort(string host, int port, string sslPublicKey, string sslPrivateKey)
        {
            var keyPair = new KeyCertificatePair(sslPublicKey, sslPrivateKey);
            var sslCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyPair});
            var serverPort = new ServerPort(host, port, sslCredentials);
            Ports.Add(serverPort);
        }

        public void AddPort(string host, int port)
        {
            Ports.Add(new ServerPort(host, port, ServerCredentials.Insecure));
        }
    }
}