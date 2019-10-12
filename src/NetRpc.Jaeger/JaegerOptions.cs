using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace NetRpc.Jaeger
{
    public class JaegerOptions
    {
        public string ServiceName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public class ServiceSwaggerOptions
    {
        public string BasePath { get; set; }
    }

    public class ClientSwaggerOptions
    {
        public string BasePath { get; set; }
    }

    internal static class Helper
    {
        public static string FormatUrl(this string url)
        {
            url = GetUrl(url, "localhost"); 
            return GetUrl(url, "0.0.0.0");
        }

        private static string GetUrl(string url, string localhostIpTag)
        {
            if (url.IndexOf(localhostIpTag, StringComparison.Ordinal) != -1)
            {
                var localIpAddress = GetLocalIPAddress();
                return url.Replace("localhost", localIpAddress);
            }
            return url;
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }

            return null;
        }
    }
}