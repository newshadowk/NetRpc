using System;

namespace NetRpc
{
    [Serializable]
    public sealed class CustomResult
    {
        public object Result { get; set; }

        public long? StreamLength { get; set; }

        public bool HasStream { get; set; }

        public CustomResult(object result, long? streamLength = null)
        {
            Result = result;
            StreamLength = streamLength;
            HasStream = result.HasStream();
        }
    }
}