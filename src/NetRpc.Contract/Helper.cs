using System;
using System.Collections.Generic;
using System.Text;

namespace NetRpc
{
    internal static class Helper
    {
        public static string ExceptionToString(Exception e)
        {
            if (e == null)
                return "";

            var msgContent = new StringBuilder("\r\n");
            msgContent.Append(GetMsgContent(e));

            var lastE = new List<Exception>();
            var currE = e.InnerException;
            lastE.Add(e);
            lastE.Add(currE);
            while (currE != null && !lastE.Contains(currE))
            {
                msgContent.Append("\r\n[InnerException]\r\n");
                msgContent.Append(GetMsgContent(e.InnerException));
                currE = currE.InnerException;
                lastE.Add(currE);
            }

            return msgContent.ToString();
        }

        private static string GetMsgContent(Exception ee)
        {
            var ret = ee.Message;
            if (!string.IsNullOrEmpty(ee.StackTrace))
                ret += "\r\n" + ee.StackTrace;
            ret += "\r\n";
            return ret;
        }
    }
}