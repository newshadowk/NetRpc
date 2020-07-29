using System;
using System.Collections.Generic;
using System.Text;

namespace NetRpc.Contract
{
    internal static class Helper
    {
        public static string ExceptionToString(Exception? e)
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

        public static string? FormatTemplate(this string? template)
        {
            if (template == null)
                return null;

            template = template.Replace('\\', '/');

            if (template.StartsWith("/"))
                template = template.Substring(1);

            if (template.EndsWith("\\"))
                template = template.Substring(0, template.Length - 1);

            return template;
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