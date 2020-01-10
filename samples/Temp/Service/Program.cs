using System;  
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;

namespace Service
{
    class Program
    {
        private const string PrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAwlS/4jNi0t12wUCgszpdZQ9hMTZGk/lTlB+iTaQWDgabtedd
LsBKssrY+HEzfbeilk3LGWaWhAVFI6nG3+9ka3IVFrVRauoj9JIdLCXvUHYeFTIt
FVN65zSzCyZi+f+Lh/DyRZGaxwTyHcCapJ1ti8Ku5cof82sVtEXOTD69k9zXFZIw
zcqAuzgGJgn/RWGPziSP4DtSTgv/3TZfzxE2fPVatdLs+e45fAREikm251reaKqO
UoGRanxFMEjMti7E66EGtebQzWzxg/divyPO5nfGbSwuuhpFAZP0bibsYWZZBuSZ
P0EnBdA7EPIc6+/K8zeK26bbJVXLSdWZ5oExxQIDAQABAoIBAHBaEz4yO1ZyV/BO
7xnwCoMdKzIKkS+IHLwstedxkJa3V1sJ0qPu5MEN6fdpaz2p58RCYcPjve3CJkEC
LUrW2xYLtQmV9uZ7TCJgP6GApSW9xJK4mbwUsZ/upf0ySlMDXaZQtaFnFzctMtXk
vwOhSO5etkOAYoyXQssg+K64L17GcPJagbHTwd3vQ2M97EKWAgJOGOUvFFlJtm3X
tlCOQ4qDTmrkB9Oezusf7mVX3v/wPDK5Ptbkpe9dHVoXs3KSu7KLYdSQr6HaD4KX
lAT0sbEGBns7zrRPlPM9dbEM+JOAo7+aJb1WjSXWiJkD4kGpdRUutprP5H9wNIzR
fnuckpkCgYEA/+lNjTV8XN+xxwjQquvNR02N34ddQttdzXYYAfZV3rJ3rapjSSnI
ghXwZFe+RzSpHPfWHc7j58c4vinoE1LuxAQ4SKvvZ2rWFMycVdMRqShRzhLjhXhl
GwqVr5t0vUMqVesIR3AVludsUaw6Oj39AQNq0Mulo1nB13WHIGCXSUsCgYEAwmX8
J/NYPHdmgnHtRPEfnBOsu8+T2ehj2fN2K/SeSi2n19agAsiMgS+TJH78Go4e0Qct
UO8LzMVc7TpChrMHQu6vbN4udiM3YpHSwZkjVNLB221A/H6mAe8xjqBiJnd6bs3h
A1Ee7Nq73RKjZ6zv1oa9M7ix+FSDDEtJVL9PFy8CgYEAw5vjCWKuspbR3p8gUOV4
vV3MaeWgJbBj3N69rxJJxLWJwRcSWSQ0/Soj268t5GOeOGRAJ/yyO8IN311M5Shp
FS9cjj/N+LCy/qb/gKKbRz9oeCn7+1NxhN/sVpRvARPL0myaoXBNMFGofC03PLmR
ICsqhHqFG31KK5TMOrVaLk0CgYBMjj80OHfqMBdVjaoj+VU6cOEYgVG3gfY4sdE6
1DW1/q7XqDURg4sZoPOwbrW3e3qVVN86vTkHZj4HSmdChqR4bmj1VbY0XgAEuGqo
RTeNwwYG9Mqf5PxZTKpWgcDFZ0327usLFFo6apWLJTjmrksxT5SZFsY3hoipQcja
MBDkjwKBgFiPW919Fqyf2sKr4+vPfew8K+lZGznA304dQzHHASo6s1ssEOOjv9XG
YbsnXRYQxgsIADpE6SDNX8CIq+Nyo1W7GDYqQDGBtQO7kJIXvr5Q1pdLL7+pMnPE
4nFkPJC4I5OajWIklNYgDJr1vZkjNPNuucUiCwfl0zfdW2zdrVvo
-----END RSA PRIVATE KEY-----";

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

        //        private const string PrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
        //MIIEogIBAAKCAQEAmTy1PKXjSeQXkzHFBlNIUEJwaAG/0h44bSl/IAbuYeAJ6mjn
        //gbxYDsimJmF5UgboFdMro8RMJ2swkiMFk1WrK6z+dsLQKPYL2I71hSkSBYkd3rAP
        //YKSgfOSrw4S/kF/DcYzoai46mQvbKzSvjXvvnSMPVcDxdO+as7J+836Y0WnjAyFt
        //f3HMbhTrx1DcsZ81zNKT68Dgv/nLSLdVPqB25J2SfLj3Zq9vPTWj9n5T7Tzq3dA+
        //BtyOEG1oN5CHKoeHu5gX8Q3VBJ2Z2QZmk91zticGWxJK5NXZMq6OUSPXpoRSKXk7
        //UwGOnjVgjIUxHLSrW9gy2tnAXDWZ5nSTwxyUKQIDAQABAoIBAAQ2oR0bypYbiXJo
        //qew0mgZq7UBO3AFhB1gpDe/JgQB78onZfJQao6k0Zy0i/Pz+Z59CAS8tlJJ45gr+
        //n4afkmdPCGgnjyWxLj40IBgbOv2f+YxH9wRHQopslzR3D/VeTLdwkmto/f97Tflw
        //J2uSftIpRqCq/3ihpfVO8SKs1nLblZfzvfX8kL2rCarBir7Zg3Gq0wX3FAk4o/OZ
        //Si1d7ZBGu6TwfrWvfeLO2apO0mmIbGW0WWlSwGpesZY2eMR6OGduujoXObIwbxQy
        //azSy9OmXHRZItB2dqM6exCrVujVeyAOMSenU6+GoMYczJ0lcqnWfu1CjmK4tWWbT
        //YWxNVwECgYEAx3gB9bUGAHWyIzTtfzYvY68gsXKFxQhH9qE5sRl+zFl1ORfWjfoy
        //DJSCgxumOMPmBu3RQAG3purvmFJpM7OKR6F53LPm+3DhkThE6oY81dK+9FXb3AQm
        //5Z8BMcgMk1ut/RTUk/IVThOvIFTICh4kHfor9QtD0gtoByER7aEXeRkCgYEAxKp4
        //slwat3rVGQJyfqtq8ug9k/EeaT39z4GGeyMgrn4LqdlbgCDiByB/WYQdnYay9UBM
        //ZqBBWlfoppFTy4Qz9kciQfEVQ6PpUGxDapAE+Cnd4zmFpfQJPowQ+pFYd9kLjUqV
        //q+hVa6WBiVMz2VqhDp5y/4i1SCHq5f7zzav/hZECgYAkP9TrWDh9AOacq1O12J0I
        //Gp1wYgWZZwXc9jPL0XxPmrpI4+Ij7yIFUz/cn7u3eTJmc/WhEi7T8MIWBnQD507r
        //8ZZ521/QthToFTfA/yAzI5d8Q9Yux7ph5GGNMHPIm9KkuZJgSJBCniUnVQD9hbi+
        //keZHJALxOw1urj3Z13qykQKBgDg94CJxwJo0IHj0gDXFY+JvlByY2e+S8ODE0+an
        //FxOPrFo4dLhhrwIGwySgaa/A87C7YJ4Aum/RsBDigqoy9oE5uoxNG52qZHDBZU42
        //PZWSs4flzVi2P2aJu9lMc5ZilbEkYUU2Pid4v2C8UJmaF3EM0ypMuDsWqAx6KtLo
        //mzxhAoGAI0+7CtgHP6I13bVGad5OEcKnyYHcPTDTcGD8tDKyQa3CiVYyaRfppqqq
        //OSFFOT5aZ+N+UV5bYcshtui0kzjcc1psBQbOPfc37MOedClRvPZcGefDD7UAt4U/
        //A7wsvDpAWxL8lGF9zLKzQxcVOtVkx2KBn1C5skHzF6j1D6ZzzNw=
        //-----END RSA PRIVATE KEY-----";

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

        //        public const string PublicKey = @"-----BEGIN CERTIFICATE-----
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

        //        public const string PrivateKey = @"-----BEGIN PRIVATE KEY-----
        //MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDf0iz4PyVPTYVh
        //aHAYY/0MSJpyzPUJpzi2NoSS5A/p05SzvmGWrnh/u/Q62wF7uUWMhRFQhVSYJP5S
        //VRicaT0/UWz5LBi2pJDd77aZs8j/RxfUVuRHycfiwQEdLnSJJhsSgtQN1KLdJdMZ
        //+94EkyMsGL1rS2uGmZKx3y/8U1fsl/tBsaIdwr7j+GKuF2dKYhYSgekbDCpL/rdp
        //orD1tyrkltP+ORk7wB7H652lD6MCIro+mvjFoqP9xjuX+YXRcKpI+ETXoa9UV4Zf
        //G0ekmCfvCnTTefV9x0iMl3L+pnU0fyeIX6N2Tk9SSqw9U/6S199uXltJ1ZIAz4e+
        //T8Ar5A2FAgMBAAECggEBANDxx5N0d6MhznuR4NuUNH5MJQs49SQu3M2V9Yufpo92
        //OuLVvwrJE6rDTwvGugrDef7llrVh0wg5uMYptNEHmerLW/z9QpD27lqR1xiD8wcF
        //WmSbPX8bCyRPY3fXj2kdjrZoNgg3Ulc7YWhnbes9ZN9ctTA990JvibfNkGFxeKpf
        //s6WVkVfoE7LTGglJ3uVGXMQjoHUQadFQXtpxyWoVgjgPK2WBMqsHsXjmo1LMwlhC
        //u9gVwZpPB2/vAsurPPmULNmrsNoyKV4rZD+VwI898GvFt+js8WOAJAi6wEpzVZJy
        //oVfcUF40l3MbA4BW9B+MQu5ojD1PKiXfGoxzecWv+90CgYEA4R38cO9YEbOeYiry
        ///ZJnIlaAOE0fJpYQKLT2m9Z7TtXEOVxVrcVvkLytDZI1OUQsu9jf6gVz9TT31kb+
        //V39JlLwfvmU2Dpfxk+wCw5PiLZX+PZuU3pu8WDkT3rm+8uRhnIPVBA+ymvjGEwYV
        //ltsNkI7UE/OpvsNZTAgv/p5mpz8CgYEA/oarfkIoEGQLmet6cQcuA1PD+l62M1mi
        //N+DElh3HK6yrd2B0tN3SAKulJO/VmbpB6dDHimv5iOZcvznRFHeDgm9QnNEU9/Kx
        //3YhH6TEMnHc9t6UAUt8beRDRKhu5Vcr4Tsp9aFzKwReXoE1yrpKnJ1umGgEi07Vl
        //QovXbv/1/jsCgYBlJHjYMOELyvzdNFjKTu3hgqXHJfdZ+dA2yd+G8t2h9O9dqjug
        //gHV4vRyvXUJmgVfr6ivoVBzeTbcQYgNCQqFHnyJQA98+vUu/3yj6wEW9n4eEHJPN
        //VIFRzgs8ZY4CSVQF40FgusDPWfh8cZ5fkfnNOi7U7AQW/mPvgjo2FuseNwKBgHSr
        //lIa+mpDQ20SF3U1cRio+RnJI4ent518Hx/Ur1zVc5AhHjZeuqmmmRIAG8/mQmFAu
        //ZJk931dyiQca4I0qL30lu2T2rk7tz7xFkPkCg85hFkhM+TYDkRtQqyBLLwx1ipbL
        //WOryxfn274kKl0wQa1CuQRhgdu9mkfEMGweFiuQrAoGATn180hYe30Js/cfnpcdA
        //hHpvAOJrvdwYZOJIiLhfifeY+9schE7FiL4H3sR3VehbjHtvsOl6MZt4Qa3WGzEs
        //OG7jPLCzo5MLKTj5Q/3EfuoQyVXugmq7jb1AqXO7bslaPKhGoeh4ZOevz0N/FMGh
        //st4KsS5NgBJEzRKLTm83s84=
        //-----END PRIVATE KEY-----";

        static async Task Main(string[] args)
        {
            await RunAsync();
        }

        static async Task RunAsync()
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    //services.AddNetRpcGrpcService(i => 
                    //    i.AddPort("localhost", 60001, PublicKey, PrivateKey));
                    services.AddNetRpcGrpcService(i =>
                        i.AddPort("0.0.0.0", 50000));
                    services.AddNetRpcServiceContract<IService, Service>(ServiceLifetime.Scoped);
                })
                .Build();
            await h.RunAsync();
        }
    }

    public class C1
    {
        
    }

    internal class Service : IService
    {
        private IService _serviceImplementation;

        //public Service(C1 c)
        //{
        //}

        public async Task<string> CallAsync(string s)
        {
            //Console.WriteLine($"CallAsync {s}");
            return "1";
        }

        public async Task<string> Call2Async(string s, Action<int> cb)
        {
            Console.WriteLine($"Call2Async {s}");
            cb.Invoke(1);
            return "ret;";
        }
    }

    public class ExMiddleware
    {
        private readonly RequestDelegate _next;

        public ExMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}