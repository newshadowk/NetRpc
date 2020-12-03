namespace NetRpc.Contract
{
    public class HttpGetAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "GET";

        public HttpGetAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpGetAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpGetAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpGetAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpHeadAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "HEAD";

        public HttpHeadAttribute() : base(_supportedMethod)
        {
        }

        public HttpHeadAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpHeadAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpHeadAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "DELETE";

        public HttpDeleteAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpDeleteAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpDeleteAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpDeleteAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpPostAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "POST";

        public HttpPostAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpPostAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpPostAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpPostAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpPatchAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "PATCH";

        public HttpPatchAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpPatchAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpPatchAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpPatchAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpPutAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "PUT";

        public HttpPutAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpPutAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpPutAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpPutAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }

    public class HttpOptionsAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "OPTIONS";

        public HttpOptionsAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpOptionsAttribute(bool obsolete)
            : base(_supportedMethod, obsolete)
        {
        }

        public HttpOptionsAttribute(string template)
            : base(_supportedMethod, template)
        {
        }

        public HttpOptionsAttribute(string template, bool obsolete)
            : base(_supportedMethod, template, obsolete)
        {
        }
    }
}