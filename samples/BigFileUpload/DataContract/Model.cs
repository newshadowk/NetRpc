using System;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using NetRpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataContract
{
    public class CObj
    {
        [Example(NameEnum.Mary)]
        public NameEnum Name { get; set; }
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

        public InnerObj InnerObj { get; set; } = new InnerObj();

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Date)}: {Date}, {nameof(InnerObj)}: {InnerObj}";
        }
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