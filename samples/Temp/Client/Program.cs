using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetRpc;
using Console = System.Console;

namespace Client
{
    class Program
    {
        //        private const string PublicKey = @"-----BEGIN CERTIFICATE-----
        //MIIC7DCCAdSgAwIBAgIQTmzgMdRuo6tNFM1jzyfLsDANBgkqhkiG9w0BAQsFADAU
        //MRIwEAYDVQQDEwlsb2NhbGhvc3QwHhcNMTkxMjE3MDIxMjEwWhcNMjQxMjE3MDAw
        //MDAwWjAUMRIwEAYDVQQDEwlsb2NhbGhvc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IB
        //DwAwggEKAoIBAQDf0iz4PyVPTYVhaHAYY/0MSJpyzPUJpzi2NoSS5A/p05SzvmGW
        //rnh/u/Q62wF7uUWMhRFQhVSYJP5SVRicaT0/UWz5LBi2pJDd77aZs8j/RxfUVuRH
        //ycfiwQEdLnSJJhsSgtQN1KLdJdMZ+94EkyMsGL1rS2uGmZKx3y/8U1fsl/tBsaId
        //wr7j+GKuF2dKYhYSgekbDCpL/rdporD1tyrkltP+ORk7wB7H652lD6MCIro+mvjF
        //oqP9xjuX+YXRcKpI+ETXoa9UV4ZfG0ekmCfvCnTTefV9x0iMl3L+pnU0fyeIX6N2
        //Tk9SSqw9U/6S199uXltJ1ZIAz4e+T8Ar5A2FAgMBAAGjOjA4MAsGA1UdDwQEAwIE
        //sDATBgNVHSUEDDAKBggrBgEFBQcDATAUBgNVHREEDTALgglsb2NhbGhvc3QwDQYJ
        //KoZIhvcNAQELBQADggEBALfkTp06XfBCyiFCtjAS2S/nHF46TIOdb6r0z7G0HSgg
        //YpFkLmllhWSAhWHkT+5baKPQA6YhcwXE8w911ZWv8ze+g48H5/nyd0PrfhADjJHw
        //FsdUmUDDqYIwiy87AK7GjeX0w4fPkNyuzChbg30Hs5cVyj+/l1Z5KyHaG7YP+9Sc
        //0kKVptHn4EAKe0pEj6qieIsCBIVL8CvDB1rOH24d45qfG02TF25/Tkgf0Y0zqly6
        //lNnhQJX2wTnd6HIrIQJ9fxRfnjCkA5K2G1w2GkPUwwRo2YRPQFmSTutG2tONnops
        //WYJb2q9GVF4h9vhJavosI1adlar05sZcLmkQCP1YtU8=
        //-----END CERTIFICATE-----";

        //        private const string PublicKey = @"-----BEGIN CERTIFICATE-----
        //MIIDJjCCAg4CAQEwDQYJKoZIhvcNAQELBQAwWDELMAkGA1UEBhMCQ04xCzAJBgNV
        //BAgMAlNDMQswCQYDVQQHDAJDRDENMAsGA1UECgwERVNPTjEMMAoGA1UECwwDQ1RD
        //MRIwEAYDVQQDDAlDVENSb290Q0EwIBcNMTkwNjExMDgxMTAxWhgPMjExOTA1MTgw
        //ODExMDFaMFgxCzAJBgNVBAYTAkNOMQswCQYDVQQIDAJTQzELMAkGA1UEBwwCQ0Qx
        //DTALBgNVBAoMBEVTT04xDDAKBgNVBAsMA0NUQzESMBAGA1UEAwwJQ1RDUm9vdENB
        //MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAmTy1PKXjSeQXkzHFBlNI
        //UEJwaAG/0h44bSl/IAbuYeAJ6mjngbxYDsimJmF5UgboFdMro8RMJ2swkiMFk1Wr
        //K6z+dsLQKPYL2I71hSkSBYkd3rAPYKSgfOSrw4S/kF/DcYzoai46mQvbKzSvjXvv
        //nSMPVcDxdO+as7J+836Y0WnjAyFtf3HMbhTrx1DcsZ81zNKT68Dgv/nLSLdVPqB2
        //5J2SfLj3Zq9vPTWj9n5T7Tzq3dA+BtyOEG1oN5CHKoeHu5gX8Q3VBJ2Z2QZmk91z
        //ticGWxJK5NXZMq6OUSPXpoRSKXk7UwGOnjVgjIUxHLSrW9gy2tnAXDWZ5nSTwxyU
        //KQIDAQABMA0GCSqGSIb3DQEBCwUAA4IBAQAzQmQqfBgqA+ENNgXsUhQikCTRiz4G
        //pyOvgQ3up7LPWsDIY4PXu5yJDoe5B5byn0TqEy+sFakgMrGt+/su/ebz8LHAU6ZL
        //Y8palk8Q7lAoTy8l4Upp6u58OBp4JsrXlv5pEnUF+58DOTDx04flF7Tqa2O0SYKd
        //QDzCVLhloIRGsYhsZTn68T4mxE0D1IY0Rq8vcUDN7Gc7P9DNXMccuN1+xDmw6WuB
        //ByfjhMbRH4gpPjerGB6TJNOXKF/E3PU671P6vASjeRcGLSHVcIKeBzF0MfmjDa1H
        //D0QfeuekYZQ59b1CIeTx1qvacFx+OOqC7qFuR1y2vbZ5jBpj2Pq6iiNj
        //-----END CERTIFICATE-----";

        private const string PublicKey = @"-----BEGIN CERTIFICATE-----
MIICVDCCAdugAwIBAgIJAPabEHfyE5yTMAoGCCqGSM49BAMCMGcxCzAJBgNVBAYT
AlpIMQwwCgYDVQQIDANycGMxDDAKBgNVBAcMA3JwYzEMMAoGA1UECgwDcnBjMQww
CgYDVQQLDANycGMxDDAKBgNVBAMMA3JwYzESMBAGCSqGSIb3DQEJARYDcnBjMB4X
DTE5MTIyMDA4NDEzOVoXDTI5MTIxNzA4NDEzOVowZzELMAkGA1UEBhMCWkgxDDAK
BgNVBAgMA3JwYzEMMAoGA1UEBwwDcnBjMQwwCgYDVQQKDANycGMxDDAKBgNVBAsM
A3JwYzEMMAoGA1UEAwwDcnBjMRIwEAYJKoZIhvcNAQkBFgNycGMwdjAQBgcqhkjO
PQIBBgUrgQQAIgNiAASKLCqPEQr6F9c/BOrIy7iDnX2KFGeH4lbxHuOnkm1YTZXI
dIK/N5yc0hL/xvibFsGXiLgIHpWDEHM8+GQkQYzBto0IEMVePHQa4w2W1RwjVywI
p7RMBignbJCJqAHW+EGjUzBRMB0GA1UdDgQWBBRSo4Gmjs18QbX+qvfc4iJTdJfS
qTAfBgNVHSMEGDAWgBRSo4Gmjs18QbX+qvfc4iJTdJfSqTAPBgNVHRMBAf8EBTAD
AQH/MAoGCCqGSM49BAMCA2cAMGQCMFEV/xLjwUh2VEx5WfeXsxCx/Q3w5lVCSf1S
ZmEO8R+IE3aiFvcRcbYS69sTA0JPgQIwLxSxmcOlRngtIFKkt7S/vcW6sJO8qS7H
oje5QvrO/6bqyqI4VquOLl2BMY0xt6p3
-----END CERTIFICATE-----";

        static async Task Main(string[] args)
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            GrpcChannelOptions o = new GrpcChannelOptions();

            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    //var ssl = new SslCredentials(PublicKey);
                    //var options = new List<ChannelOption>();
                    //options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride, "CTCRootCA"));
                    //var channel = new Channel("localhost", 50001, ssl, options);
                    //var channel = new Channel("localhost", 60001, ssl);
                    //var channel = new Channel("localhost", 50001, new SslCredentials());
                    services.AddNetRpcGrpcClient(i =>
                    {
                        i.Url = "http://localhost:5000";
                        i.ChannelOptions.MaxReceiveMessageSize = 20 * 1024 * 1024; // 2 MB
                        i.ChannelOptions.MaxSendMessageSize = 20 * 1024 * 1024; // 5 MB
                    });

                    services.AddNetRpcClientContract<IService>();
                    services.AddHostedService<MyHost>();
                })
                .Build();

            await h.RunAsync();
        }
    }

    public class MyHost : IHostedService
    {
        private readonly IService _service;

        public MyHost(IService service)
        {
            _service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("start");
            //try
            //{
            //    await _service.CallAsync("123");
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}

            //await _service.Call2Async("123", async i => Console.WriteLine(i), cts.Token);
            Task.Run(async () =>
            {
                string s = "";
                for (int i = 0; i < 1024; i++)
                {
                    s += "1";
                }

                List<string> list = new List<string>();
                for (int i = 0; i < 1024 * 1024 * 3; i++)
                {
                    list.Add(s);
                }


            Console.WriteLine("BigDataAsync send start");
            var t = await _service.BigDataAsync(list);
            Console.WriteLine("BigDataAsync send end");

                //CancellationTokenSource cts = new CancellationTokenSource();
                //while (true)
                //{
                //    using (var s = File.OpenRead(@"D:\TestFile\10MB.db"))
                //    {
                //        try
                //        {
                //            Console.WriteLine("call start");
                //            var r = await _service.Call3Async(s, async i => Console.WriteLine(i), cts.Token);
                //            MemoryStream ms = new MemoryStream();
                //            r.Stream.CopyTo(ms);
                //            r.Stream.Dispose();
                //            Console.WriteLine("call end");
                //        }
                //        catch (Exception e)
                //        {
                //            Console.WriteLine(e);
                //            throw;
                //        }
                //    }
                //}
            });
            
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}