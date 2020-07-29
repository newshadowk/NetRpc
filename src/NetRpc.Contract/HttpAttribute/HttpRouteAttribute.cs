using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class HttpRouteAttribute : Attribute, IRouteTemplateProvider
    {
        public HttpRouteAttribute(string template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            Template = Template.FormatTemplate();
        }

        public string? Template { get; }
    }
}