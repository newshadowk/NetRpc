using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class HttpRouteAttribute : Attribute, IRouteTemplateProvider
    {
        public HttpRouteAttribute(string template)
            : this(template, false)
        {
        }

        public HttpRouteAttribute(string template, bool obsolete)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            Template = Template.FormatTemplate();
            Obsolete = obsolete;
        }

        public string? Template { get; }

        public bool Obsolete { get; }
    }
}