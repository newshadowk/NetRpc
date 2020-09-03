using System;
using System.Collections.Generic;
using System.Text;

namespace NetRpc.Http.Abstract
{
    public class NullInjectSwaggerHtml: IInjectSwaggerHtml
    {
        public string InjectHtml(string html)
        {
            return html;
        }
    }
}
