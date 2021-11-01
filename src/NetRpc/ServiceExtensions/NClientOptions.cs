using System.Collections.Generic;

namespace NetRpc
{
    public class NClientOptions
    {
        /// <summary>
        /// Default value is 1200000.
        /// </summary>
        public int TimeoutInterval { get; set; } = 1200000;

        /// <summary>
        /// Default value is 10000.
        /// </summary>
        public int HearbeatInterval { get; set; } = 10000;

        /// <summary>
        /// Forward header from coming side, default value is false.
        /// </summary>
        public bool ForwardHeader { get; set; }

        /// <summary>
        /// Forward header from coming side by keys.
        /// </summary>
        public List<string> ForwardHeaderKeys { get; set; } = new();
    }
}