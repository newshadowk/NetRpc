namespace NetRpc
{
    public sealed class ConnectionInfo
    {
        public ChannelType ChannelType { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{ChannelType}, {Host}:{Port}, {Description}";
        }
    }
}