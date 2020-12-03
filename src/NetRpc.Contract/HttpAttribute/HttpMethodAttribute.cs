using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class HttpMethodAttribute : Attribute, IRouteTemplateProvider
    {
        protected HttpMethodAttribute(string httpMethod)
            : this(httpMethod, null)
        {
        }

        protected HttpMethodAttribute(string httpMethod, bool obsolete)
            : this(httpMethod, null, obsolete)
        {
        }

        protected HttpMethodAttribute(string httpMethod, string? template)
           : this(httpMethod, template, false)
        {
        }

        protected HttpMethodAttribute(string httpMethod, string? template, bool obsolete)
        {
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));
            Obsolete = obsolete;
            HttpMethod = httpMethod;
            Template = template.FormatTemplate();
        }

        public string HttpMethod { get; }

        public string? Template { get; }

        public bool Obsolete { get; }

        public override string ToString()
        {
            return HttpMethod;
        }
    }

    public static class HttpMethodAttributeFactory
    {
        public static HttpMethodAttribute Create(string httpMethod, bool obsolete)
        {
            return httpMethod switch
            {
                "GET" => new HttpGetAttribute(obsolete),
                "HEAD" => new HttpHeadAttribute(obsolete),
                "DELETE" => new HttpDeleteAttribute(obsolete),
                "POST" => new HttpPostAttribute(obsolete),
                "PATCH" => new HttpPatchAttribute(obsolete),
                "PUT" => new HttpPutAttribute(obsolete),
                "OPTIONS" => new HttpOptionsAttribute(obsolete),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}