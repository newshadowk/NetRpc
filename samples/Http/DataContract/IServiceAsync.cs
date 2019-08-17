using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Http;

namespace DataContract
{
    public interface IServiceAsync
    {
        /// <summary>
        /// summary of Call
        /// </summary>
        /// <response code="201">Returns the newly created item</response>
        //[ProducesResponseType(typeof(string), 201)]
        Task<CustomObj> Call(string p1, int p2);

        /// <summary>
        /// summary of Call
        /// </summary>
        /// <response code="400">CustomException error.</response>
        /// <response code="401">CustomException2 error</response>
        [NetRpcProducesResponseType(typeof(CustomException), 400)]
        [NetRpcProducesResponseType(typeof(CustomException2), 401)]
        Task CallByCustomExceptionAsync();

        /// <response code="701">return the pain text.</response>
        Task CallByResponseTextExceptionAsync();

        /// <summary>
        /// summary of EchoStreamAsync
        /// </summary>
        Task<Stream> EchoStreamAsync(Stream stream);

        /// <summary>
        /// summary of GetComplexStreamAsync
        /// </summary>
        Task<ComplexStream> GetComplexStreamAsync();

        /// <summary>
        /// summary of ComplexCallAsync
        /// </summary>
        Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}