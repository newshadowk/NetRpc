using System;
using System.Net;
using System.Net.Sockets;
using Jaeger;
using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.Jaeger
{
    internal static class Helper
    {
        public static string FormatUrl(this string url)
        {
            url = GetUrl(url, "localhost"); 
            return GetUrl(url, "0.0.0.0");
        }

        public static string GetRequestUrl(string hostPath, string apiPath, string contractPath, string methodPath)
        {
            string apiStr = "";
            if (!string.IsNullOrEmpty(apiPath))
                apiStr = $"_{apiPath}";

            return $"{hostPath}/index.html#/{contractPath}/post{apiStr}_{contractPath}_{methodPath}";
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