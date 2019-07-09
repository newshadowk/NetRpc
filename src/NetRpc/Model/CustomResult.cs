using System;
using System.IO;

namespace NetRpc
{
    [Serializable]
    public sealed class CustomResult
    {
        public object Result { get; set; }

        public long? StreamLength { get; set; }

        public bool HasStream { get; set; }

        public CustomResult(object result, bool hasStream, long? streamLength = null)
        {
            Result = result;
            HasStream = hasStream;
            StreamLength = streamLength;
        }
    }
}