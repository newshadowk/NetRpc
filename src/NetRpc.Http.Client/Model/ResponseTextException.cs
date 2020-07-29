using System;
using System.Runtime.Serialization;
using NetRpc.Contract;

namespace NetRpc.Http.Client
{
    [Serializable]
    public class ResponseTextException : Exception
    {
        public string? Text { get; set; }

        public int StatusCode { get; set; }

        public ResponseTextException(string text, int statusCode)
        {
            Text = text;
            StatusCode = statusCode;
        }

        protected ResponseTextException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.SetObjectData(info);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            this.GetObjectData(info);
        }

        public ResponseTextException()
        {
        }

        public override string ToString()
        {
            return $"{StatusCode}, {Text}";
        }
    }
}