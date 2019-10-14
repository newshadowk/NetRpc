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
        public string HostPath { get; set; }
        public string ApiPath { get; set; }
    }

    public class ClientSwaggerOptions
    {
        public string HostPath { get; set; }
        public string ApiPath { get; set; }
    }
}