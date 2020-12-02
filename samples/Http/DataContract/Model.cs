using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using NetRpc.Contract;

namespace DataContract
{
    /// <summary>
    /// CObj des
    /// </summary>
    public class CObj
    {
        /// <summary>
        /// NameEnum Name
        /// </summary>
        [Example(NameEnum.Mary)]
        public NameEnum Name { get; set; }

        /// <summary>
        /// Age des
        /// </summary>
        [Example("Example2 age")]
        public string Age { get; set; }
    }

    /// <summary>
    /// a CustomObj
    /// </summary>
    public class CustomObj
    {
        /// <summary>
        /// a Name
        /// </summary>
        public NameEnum Name { get; set; }

        /// <summary>
        /// a Date
        /// </summary>
        public DateTime Date { get; set; }

        /// <example>00000000-0000-0000-0000-000000000000</example>
        [DefaultValue("This defalut value of P1")]
        public string P1 { get; set; }

        [Example(null)]
        public int? I1 { get; set; }

        public InnerObj InnerObj { get; set; } = new();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Date)}: {Date}, {nameof(InnerObj)}: {InnerObj}";
        }
    }

    public class SimObj
    {
        public string P1 { get; set; }
        public int P2 { get; set; }
        public DateTime P3 { get; set; }
    }

    /// <example>Mary</example>
    public enum NameEnum
    {
        John,
        Mary
    }

    public class InnerObj
    {
        public CustomObj CustomObj { get; set; }

        public string P1 { get; set; }

        public override string ToString()
        {
            return $"{nameof(CustomObj)}: {CustomObj}, {nameof(P1)}: {P1}";
        }
    }

    public class CustomCallbackObj
    {
        public int Progress { get; set; }
    }

    public class ComplexStream
    {
        [JsonIgnore]
        public Stream Stream { get; set; }

        public string StreamName { get; set; } //the property will map to file name.

        public InnerObj InnerObj { get; set; }
    }

    /// <summary>
    /// summary of CustomException
    /// </summary>
    //[Serializable]
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

        //protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        //{
        //}
    }

    public class CustomException2 : Exception
    {
        public CustomException2()
        {
        }

        protected CustomException2(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}