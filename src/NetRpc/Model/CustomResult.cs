using System;

namespace NetRpc
{
    [Serializable]
    public sealed class CustomResult
    {
        public object? Result { get; set; }

        public long StreamLength { get; set; }

        public bool HasStream { get; set; }

        public bool IsImages { get; set; }

        public CustomResult(object? result, bool hasStream, bool isImages, long streamLength)
        {
            Result = result;
            HasStream = hasStream;
            IsImages = isImages;
            StreamLength = streamLength;
        }
    }
}