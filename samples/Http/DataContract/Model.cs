using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataContract
{
    public class CustomObj
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public CustomObj2 CustomObj2Value { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Date)}: {Date}";
        }
    }

    public class CustomObj2
    {
        public string P1 { get; set; }
    }

    public class CustomCallbackObj
    {
        public int Progress { get; set; }
    }

    public class ComplexStream
    {
        public Stream Stream { get; set; }

        public string StreamName { get; set; }
    }

    public class CustomException : Exception
    {
        public string P1 { get; set; }

        public string P2 { get; set; }

        public CustomException(string message, string p1, string p2) : base(message)
        {
            P1 = p1;
            P2 = p2;
        }

        public CustomException()
        {
        }

        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}