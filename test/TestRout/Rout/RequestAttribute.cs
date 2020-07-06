namespace NetRpc
{
    public class HttpGetAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "GET";

        public HttpGetAttribute()
            : base(_supportedMethod)
        {
        }

        public HttpGetAttribute(string template)
            : base(_supportedMethod, template)
        {
        }
    }

    public class HttpHeadAttribute : HttpMethodAttribute
    {
        private static readonly string _supportedMethod = "HEAD";

        public HttpHeadAttribute() : base(_supportedMethod)
        {
        }

        public HttpHeadAttribute(string template)
            : base(_supportedMethod, template)
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

        public HttpDeleteAttribute(string template)
            : base(_supportedMethod, template)
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

        public HttpPostAttribute(string template)
            : base(_supportedMethod, template)
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

        public HttpPatchAttribute(string template)
            : base(_supportedMethod, template)
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

        public HttpPutAttribute(string template)
            : base(_supportedMethod, template)
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

        public HttpOptionsAttribute(string template)
            : base(_supportedMethod, template)
        {
        }
    }
}