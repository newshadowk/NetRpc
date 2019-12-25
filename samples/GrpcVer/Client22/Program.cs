using System;
using System.Net.Http;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using GrpcService1;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        private const string PublicKey = @"-----BEGIN CERTIFICATE-----
MIIC7DCCAdSgAwIBAgIQTmzgMdRuo6tNFM1jzyfLsDANBgkqhkiG9w0BAQsFADAU
MRIwEAYDVQQDEwlsb2NhbGhvc3QwHhcNMTkxMjE3MDIxMjEwWhcNMjQxMjE3MDAw
MDAwWjAUMRIwEAYDVQQDEwlsb2NhbGhvc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IB
DwAwggEKAoIBAQDf0iz4PyVPTYVhaHAYY/0MSJpyzPUJpzi2NoSS5A/p05SzvmGW
rnh/u/Q62wF7uUWMhRFQhVSYJP5SVRicaT0/UWz5LBi2pJDd77aZs8j/RxfUVuRH
ycfiwQEdLnSJJhsSgtQN1KLdJdMZ+94EkyMsGL1rS2uGmZKx3y/8U1fsl/tBsaId
wr7j+GKuF2dKYhYSgekbDCpL/rdporD1tyrkltP+ORk7wB7H652lD6MCIro+mvjF
oqP9xjuX+YXRcKpI+ETXoa9UV4ZfG0ekmCfvCnTTefV9x0iMl3L+pnU0fyeIX6N2
Tk9SSqw9U/6S199uXltJ1ZIAz4e+T8Ar5A2FAgMBAAGjOjA4MAsGA1UdDwQEAwIE
sDATBgNVHSUEDDAKBggrBgEFBQcDATAUBgNVHREEDTALgglsb2NhbGhvc3QwDQYJ
KoZIhvcNAQELBQADggEBALfkTp06XfBCyiFCtjAS2S/nHF46TIOdb6r0z7G0HSgg
YpFkLmllhWSAhWHkT+5baKPQA6YhcwXE8w911ZWv8ze+g48H5/nyd0PrfhADjJHw
FsdUmUDDqYIwiy87AK7GjeX0w4fPkNyuzChbg30Hs5cVyj+/l1Z5KyHaG7YP+9Sc
0kKVptHn4EAKe0pEj6qieIsCBIVL8CvDB1rOH24d45qfG02TF25/Tkgf0Y0zqly6
lNnhQJX2wTnd6HIrIQJ9fxRfnjCkA5K2G1w2GkPUwwRo2YRPQFmSTutG2tONnops
WYJb2q9GVF4h9vhJavosI1adlar05sZcLmkQCP1YtU8=
-----END CERTIFICATE-----";
        static async Task Main(string[] args)
        {
            HttpClient c = new HttpClient();
            var res = await c.GetAsync("http://localhost:5000/11");

            //var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 5000, ChannelCredentials.Insecure));
            var p = NetRpcManager.CreateClientProxy<IService>("localhost", 5001, PublicKey);
            //await p.Proxy.Call("hello world.");

            var channel = new Channel("localhost", 5000, ChannelCredentials.Insecure);
            var client = new Greeter.GreeterClient(channel);
            var r = await client.SayHelloAsync(new HelloRequest() {Name = "n1"});
            Console.Read();
        }
    }
}
