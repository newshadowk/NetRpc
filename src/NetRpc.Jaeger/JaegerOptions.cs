namespace NetRpc.Jaeger
{
    public class JaegerOptions
    {
        public string ServiceName { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
    }

    public class ServiceSwaggerOptions
    {
        public string HostPath { get; set; } = null!;
        public string ApiPath { get; set; } = null!;
    }

    /// <summary>
    /// Support OptionName.
    /// </summary>
    public class ClientSwaggerOptions
    {
        public string HostPath { get; set; } = null!;
        public string ApiPath { get; set; } = null!;
    }
}