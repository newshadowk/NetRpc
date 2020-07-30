using NetRpc.Contract;

namespace NetRpc
{
    public sealed class ConnectionInfo
    {
        public ChannelType ChannelType { get; set; }
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string Description { get; set; } = null!;

        public override string ToString()
        {
            return $"{ChannelType}, {Host}:{Port}, {Description}";
        }
    }
}