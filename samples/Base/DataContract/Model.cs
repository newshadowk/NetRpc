using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataContract
{
    [Serializable]
    public class CustomObj
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Date)}: {Date}";
        }
    }

    [Serializable]
    public class CustomCallbackObj
    {
        public int Progress { get; set; }
    }

    [Serializable]
    public class ComplexStream
    {
        [field: NonSerialized]
        public Stream Stream { get; set; }

        public string OtherInfo { get; set; }
    }

    [Serializable]
    public class CustomException : Exception
    {
        public CustomException()
        {
        }

        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}