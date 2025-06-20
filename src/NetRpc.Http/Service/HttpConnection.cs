using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetRpc.Http.Client;
using Polly;

namespace NetRpc.Http;

internal sealed class HttpConnection
{
    private readonly HttpContext _context;

    public HttpConnection(HttpContext context)
    {
        _context = context;
    }

    public async Task SendWithStreamAsync(ActionExecutingContext aec, CustomResult result, Stream stream, string? streamName)
    {
        var emptyActionDescriptor = new ActionDescriptor();
        var routeData = _context.GetRouteData();
        var actionContext = new ActionContext(_context, routeData, emptyActionDescriptor);

        FileStreamResult fRet;

        if (result.IsImage)
        {
            //images no FileDownloadName
            string ext;
            if (string.IsNullOrEmpty(streamName))
                ext = "image/jpeg";
            else
                ext = MimeTypeMap.GetMimeType(Path.GetExtension(streamName));
            fRet = new FileStreamResult(stream, ext);
        }
        else
        {
            fRet = new FileStreamResult(stream, MimeTypeMap.GetMimeType(Path.GetExtension(streamName)))
            {
                FileDownloadName = streamName
            };
        }

        if (result.Result is not System.IO.Stream)
        {
            var json = result.Result.ToDtoJson();
            json = HttpUtility.UrlEncode(json, Encoding.UTF8);
#pragma warning disable ASP0019
            _context.Response.Headers.Add(ClientConst.CustomResultHeaderKey, json);
#pragma warning restore ASP0019
        }

        var executor = new FileStreamResultExecutor(NullLoggerFactory.Instance);

        //handle stream
        aec.OnSendResultStreamStarted();
        try
        {
            await executor.ExecuteAsync(actionContext, fRet);
        }
        catch
        {
            aec.OnSendResultStreamEndOrFault();
            throw;
        }

        aec.OnSendResultStreamEndOrFault();
    }

    public async Task SendAsync(Result result)
    {
        _context.Response.ContentType = "application/json; charset=utf-8";
        _context.Response.StatusCode = result.StatusCode;
        var s = result.ToJson() ?? "";
        await _context.Response.WriteAsync(s);
        _context.Items["Result"] = s;
    }
}