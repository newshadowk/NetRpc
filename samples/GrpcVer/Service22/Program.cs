using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Service
{
    class Program
    {
        private const string PrivateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDf0iz4PyVPTYVh
aHAYY/0MSJpyzPUJpzi2NoSS5A/p05SzvmGWrnh/u/Q62wF7uUWMhRFQhVSYJP5S
VRicaT0/UWz5LBi2pJDd77aZs8j/RxfUVuRHycfiwQEdLnSJJhsSgtQN1KLdJdMZ
+94EkyMsGL1rS2uGmZKx3y/8U1fsl/tBsaIdwr7j+GKuF2dKYhYSgekbDCpL/rdp
orD1tyrkltP+ORk7wB7H652lD6MCIro+mvjFoqP9xjuX+YXRcKpI+ETXoa9UV4Zf
G0ekmCfvCnTTefV9x0iMl3L+pnU0fyeIX6N2Tk9SSqw9U/6S199uXltJ1ZIAz4e+
T8Ar5A2FAgMBAAECggEBANDxx5N0d6MhznuR4NuUNH5MJQs49SQu3M2V9Yufpo92
OuLVvwrJE6rDTwvGugrDef7llrVh0wg5uMYptNEHmerLW/z9QpD27lqR1xiD8wcF
WmSbPX8bCyRPY3fXj2kdjrZoNgg3Ulc7YWhnbes9ZN9ctTA990JvibfNkGFxeKpf
s6WVkVfoE7LTGglJ3uVGXMQjoHUQadFQXtpxyWoVgjgPK2WBMqsHsXjmo1LMwlhC
u9gVwZpPB2/vAsurPPmULNmrsNoyKV4rZD+VwI898GvFt+js8WOAJAi6wEpzVZJy
oVfcUF40l3MbA4BW9B+MQu5ojD1PKiXfGoxzecWv+90CgYEA4R38cO9YEbOeYiry
/ZJnIlaAOE0fJpYQKLT2m9Z7TtXEOVxVrcVvkLytDZI1OUQsu9jf6gVz9TT31kb+
V39JlLwfvmU2Dpfxk+wCw5PiLZX+PZuU3pu8WDkT3rm+8uRhnIPVBA+ymvjGEwYV
ltsNkI7UE/OpvsNZTAgv/p5mpz8CgYEA/oarfkIoEGQLmet6cQcuA1PD+l62M1mi
N+DElh3HK6yrd2B0tN3SAKulJO/VmbpB6dDHimv5iOZcvznRFHeDgm9QnNEU9/Kx
3YhH6TEMnHc9t6UAUt8beRDRKhu5Vcr4Tsp9aFzKwReXoE1yrpKnJ1umGgEi07Vl
QovXbv/1/jsCgYBlJHjYMOELyvzdNFjKTu3hgqXHJfdZ+dA2yd+G8t2h9O9dqjug
gHV4vRyvXUJmgVfr6ivoVBzeTbcQYgNCQqFHnyJQA98+vUu/3yj6wEW9n4eEHJPN
VIFRzgs8ZY4CSVQF40FgusDPWfh8cZ5fkfnNOi7U7AQW/mPvgjo2FuseNwKBgHSr
lIa+mpDQ20SF3U1cRio+RnJI4ent518Hx/Ur1zVc5AhHjZeuqmmmRIAG8/mQmFAu
ZJk931dyiQca4I0qL30lu2T2rk7tz7xFkPkCg85hFkhM+TYDkRtQqyBLLwx1ipbL
WOryxfn274kKl0wQa1CuQRhgdu9mkfEMGweFiuQrAoGATn180hYe30Js/cfnpcdA
hHpvAOJrvdwYZOJIiLhfifeY+9schE7FiL4H3sR3VehbjHtvsOl6MZt4Qa3WGzEs
OG7jPLCzo5MLKTj5Q/3EfuoQyVXugmq7jb1AqXO7bslaPKhGoeh4ZOevz0N/FMGh
st4KsS5NgBJEzRKLTm83s84=
-----END PRIVATE KEY-----";

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
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i =>
                    {
                        i.AddPort("0.0.0.0", 5001, PublicKey, PrivateKey);
                    });
                   
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Build();
            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }

        public Task<Stream> Echo(Stream s)
        {
            throw new NotImplementedException();
        }
    }
}