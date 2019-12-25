using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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

namespace NetRpc.Http
{
    internal sealed class HttpConnection
    {
        private readonly HttpContext _context;
        private readonly IHubContext<CallbackHub, ICallback> _hub;
        private readonly ILogger _logger;

        public HttpConnection(HttpContext context, IHubContext<CallbackHub, ICallback> hub, ILogger logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        //public ProxyStream Stream
        //{
        //    set
        //    {
        //        value.Progress += (s, e) =>
        //        {
        //            double percent;
        //            if (value.Length == 0)
        //                percent = 0;
        //            else
        //            {
        //                var p = (double)e / value.Length;
        //                if (p == 0)
        //                    return;

        //                percent = p * 100;
        //            }

        //            UploadProgress(percent, e, )
        //        };
        //    }
        //}

        private void Value_Progress(object sender, long e)
        {
            throw new NotImplementedException();
        }

        public async Task CallBack(object callbackObj)
        {
            try
            {
                if (ConnectionId != null)
                    await _hub.Clients.Client(ConnectionId).Callback(CallId, callbackObj.ToDtoJson());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        private async Task UploadProgress(double percent, long completedSize, long speed)
        {
            try
            {
                if (ConnectionId != null)
                    await _hub.Clients.Client(ConnectionId).UploadProgress(CallId, percent, completedSize, speed, NetRpc.Helper.SizeSuffix(speed));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        public async Task SendWithStreamAsync(CustomResult result, Stream stream, string streamName)
        {
            var emptyActionDescriptor = new ActionDescriptor();
            var routeData = _context.GetRouteData() ?? new RouteData();
            var actionContext = new ActionContext(_context, routeData, emptyActionDescriptor);

            var fRet = new FileStreamResult(stream, MimeTypeMap.GetMimeType(Path.GetExtension(streamName)))
            {
                FileDownloadName = streamName
            };

            if (!(result.Result is Stream))
            {
                var json = result.Result.ToDtoJson();
                json = HttpUtility.UrlEncode(json, Encoding.UTF8);
                _context.Response.Headers.Add(ClientConstValue.CustomResultHeaderKey, json);
            }

            var executor = new FileStreamResultExecutor(NullLoggerFactory.Instance);
            await executor.ExecuteAsync(actionContext, fRet);
        }

        public async Task SendAsync(Result result)
        {
            _context.Response.ContentType = "application/json; charset=utf-8";
            _context.Response.StatusCode = result.StatusCode;
            string s;
            if (result.IsPainText)
                s = result.Ret.ToString();
            else
                s = result.Ret.ToDtoJson();
            await _context.Response.WriteAsync(s ?? "");
        }
    }

    //public class ProgressCounter
    //{
    //    private readonly BusyTimer _tSpeed = new BusyTimer(1000);
    //    private long _currSize;
    //    private readonly object _lockObj = new object();
    //    private readonly long _totalSize;
    //    private int _speed;
    //    private TimeSpan _leftTimeSpan;
    //    private readonly Queue<long> _qOldSize = new Queue<long>();
    //    private const int GapSecs = 3;

    //    public int Speed
    //    {
    //        get
    //        {
    //            lock (_lockObj)
    //            {
    //                return _speed;
    //            }
    //        }
    //    }

    //    public TimeSpan LeftTime
    //    {
    //        get
    //        {
    //            lock (_lockObj)
    //            {
    //                return _leftTimeSpan;
    //            }
    //        }
    //    }

    //    public ProgressCounter(long totalSize)
    //    {
    //        _totalSize = totalSize;
    //        _tSpeed.Start();
    //        _tSpeed.Elapsed += TSpeedElapsed;
    //    }

    //    private void TSpeedElapsed(object sender, ElapsedEventArgs e)
    //    {
    //        lock (_lockObj)
    //        {
    //            if (_qOldSize.Count == 0)
    //            {
    //                _qOldSize.Enqueue(_currSize);
    //                return;
    //            }

    //            var (spanSecs, oldSize) = GetDataFromQueue(_qOldSize);
    //            var (speed, leftTimeSpan) = Count(_currSize, _totalSize, oldSize, spanSecs);
    //            _speed = speed;
    //            _leftTimeSpan = leftTimeSpan;
    //            _qOldSize.Enqueue(_currSize);
    //        }
    //    }

    //    private static (int spanSecs, long oldSize) GetDataFromQueue(Queue<long> q)
    //    {
    //        if (q.Count < GapSecs)
    //            return (q.Count, q.Peek());

    //        if (q.Count == GapSecs)
    //            return (3, q.Dequeue());

    //        throw new ArgumentOutOfRangeException("", $"ProgessCounter.GetDataFromQueue() failed. q.Count is greater than {GapSecs} ");
    //    }

    //    private static (int speed, TimeSpan leftTimeSpan) Count(long currSize, long totalSize, long oldSize, int spanSecs)
    //    {
    //        var speed = (int)(currSize - oldSize) / spanSecs;
    //        long leftSec;
    //        if (speed == 0)
    //            leftSec = (long)TimeSpan.MaxValue.TotalSeconds;
    //        else
    //            leftSec = (totalSize - currSize) / speed;
    //        var leftTimeSpan = TimeSpan.FromSeconds(leftSec);

    //        return (speed, leftTimeSpan);
    //    }

    //    public void Update(long currSize)
    //    {
    //        lock (_lockObj)
    //        {
    //            _currSize = currSize;
    //        }
    //    }
    //}
}