using System;
using System.IO;
using System.Threading.Tasks;
using NetRpc;

namespace DataContract
{
    public class Ex1 : Exception
    {
        
    }

    public class Ex2 : Exception
    {

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

    [FaultException(typeof(Ex1), 402, "ex1 error")]
    public interface I2 : IService
    {

    }

    public interface IService
    {
        /// <summary>
        /// summary of Call
        /// </summary>
        /// <response code="403">Ex2 error</response>
        [FaultException(typeof(Ex2), 403)]
        Task Call3(Stream stream, int index, Action<double> prog);

        Task Call();
    }
}