namespace NetRpc
{
    public class NetRpcClientOption
    {
        /// <summary>
        /// Default value is false.
        /// </summary>
        public bool IsWrapFaultException { get; set; }

        /// <summary>
        /// Default value is 1200000.
        /// </summary>
        public int TimeoutInterval { get; set; } = 1200000;

        /// <summary>
        /// Default value is 10000.
        /// </summary>
        public int HearbeatInterval { get; set; } = 10000;
    }
}