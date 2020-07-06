using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class HttpMethodAttribute : Attribute, IRouteTemplateProvider
    {
        protected HttpMethodAttribute(string httpMethod)
            : this(httpMethod, null)
        {
        }

        protected HttpMethodAttribute(string httpMethod, string template)
        {
            if (httpMethod == null)
                throw new ArgumentNullException(nameof(httpMethod));
            HttpMethod = httpMethod;
            Template = template.FormatTemplate();
        }

        public string HttpMethod { get; }

        public string Template { get; }
    }
}