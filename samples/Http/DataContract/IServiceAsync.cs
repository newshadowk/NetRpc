using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetRpc;

namespace DataContract
{
    [HttpRoute("Service", true)]
    public interface IServiceAsync
    {
        /// <summary>
        /// summary of Call
        /// </summary>
        /// <response code="201">Returns the newly created item</response>
        [HttpRoute("Service1/Call2")]
        Task<CustomObj> CallAsync(string p1, int p2);

        /// <summary>
        /// summary of Call
        /// </summary>
        /// <response code="401">CustomException error.</response>
        /// <response code="402">CustomException2 error</response>
        [FaultException(typeof(CustomException), 401)]
        [FaultException(typeof(CustomException2), 402)]
        Task CallByCustomExceptionAsync();

        Task CallByDefaultExceptionAsync();

        Task CallByCancelAsync(CancellationToken token);

        /// <response code="701">return the pain text.</response>
        [ResponseText(701)]
        Task CallByResponseTextExceptionAsync();

        Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Action<CustomCallbackObj> cb, CancellationToken token);
    }
}