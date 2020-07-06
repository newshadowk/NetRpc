using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace NetRpc
{
    public class MethodRoute
    {
        public HttpRoutInfo DefaultRout { get; }

        public ReadOnlyCollection<HttpRoutInfo> Routs { get; }

        public ReadOnlyCollection<HttpRoutInfo> SwaggerRouts { get; }

        public MethodRoute(Type contractType, MethodInfo methodInfo)
        {
            //HttpRoutInfo
            var list = GetRouts(contractType, methodInfo);
            Routs = new ReadOnlyCollection<HttpRoutInfo>(list);

            var post = list.FirstOrDefault(i => i.HttpMethods.Any(j => j == "POST"));
            if (post != null)
                DefaultRout = post;
            else
                DefaultRout = list[0];
            SwaggerRouts = Routs.Count > 0 ? Routs : new ReadOnlyCollection<HttpRoutInfo>(new List<HttpRoutInfo> { DefaultRout });
        }

        private static IList<HttpRoutInfo> GetRouts(Type contractType, MethodInfo methodInfo)
        {
            var contractTrimAsync = contractType.IsDefined(typeof(HttpTrimAsyncAttribute));
            var methodTrimAsync = methodInfo.IsDefined(typeof(HttpTrimAsyncAttribute)) || contractTrimAsync;
            
            var contractRoutes = contractType.GetCustomAttributes<HttpRouteAttribute>(true);
            var tag = contractType.GetCustomAttribute<SwaggerTagAttribute>(true);
            var methodHttpMethods = methodInfo.GetCustomAttributes<HttpMethodAttribute>(true).ToList();
            var methodRoutes = methodInfo.GetCustomAttributes<HttpRouteAttribute>(true).ToList();
            string contractTag = GetTag(tag, contractType, contractTrimAsync);

            //tempInfos
            var tempInfos = new List<TempInfo>();
            foreach (var r in contractRoutes)
            {
                foreach (var m in methodHttpMethods)
                {
                    tempInfos.Add(new TempInfo(
                        contractType, 
                        methodInfo, 
                        r.Template,
                        m.Template,
                        m.HttpMethod,
                        contractTrimAsync,
                        methodTrimAsync
                    ));
                }

                foreach (var mr in methodRoutes)
                {
                    tempInfos.Add(new TempInfo(
                        contractType,
                        methodInfo,
                        r.Template,
                        mr.Template,
                        null,
                        contractTrimAsync,
                        methodTrimAsync
                    ));
                }
            }

            //
            var ret = new List<HttpRoutInfo>();
            foreach (var group in tempInfos.GroupBy(i => i.Path))
            {
                 var r = new HttpRoutInfo(contractTag, group.Key);
                 foreach (TempInfo info in group)
                 {
                     if (info.Method != null)
                         r.HttpMethods.Add(info.Method);
                 }
                ret.Add(r);
            }

            return ret;
        }

        public HttpRoutInfo MatchPath(string rawPath, string method)
        {
            return Routs.FirstOrDefault(i => i.IsMatch(rawPath, method));
        }

        private static string GetTag(SwaggerTagAttribute tag, Type contractType, bool contractTrimAsync)
        {
            string contractPath;
            //contractPath
            if (tag == null || string.IsNullOrWhiteSpace(tag.Tag))
                contractPath = contractType.Name;
            else
                contractPath = tag.Tag;

            if (contractTrimAsync)
                contractPath = contractPath.TrimEndString("Async");
            return contractPath;
        }

        private class TempInfo
        {
            public string ContractTemplate { get; }
            public string MethodTemplate { get; }
            public string Method { get; }
            public bool ContractTrimAsync { get; }
            public bool MethodTrimAsync { get; }
            public string Path { get; }

            public TempInfo(Type contractType, MethodInfo methodInfo, string contractTemplate, string methodTemplate, string method, bool contractTrimAsync, bool methodTrimAsync)
            {
                ContractTemplate = contractTemplate;
                MethodTemplate = methodTemplate;
                Method = method;
                ContractTrimAsync = contractTrimAsync;
                MethodTrimAsync = methodTrimAsync;

                string contractPath;
                if (contractTemplate == null)
                    contractPath = contractType.Name;
                else
                {
                    if (contractTemplate.StartsWith("/"))
                        contractTemplate = contractTemplate.Substring(1);
                    contractPath = contractTemplate;
                }

                if (contractTrimAsync)
                    contractPath = contractPath.TrimEndString("Async");

                string methodPath;
                if (methodTemplate == null)
                    methodPath = methodInfo.Name;
                else
                    methodPath = methodTemplate;

                if (contractTrimAsync)
                    methodPath = contractPath.TrimEndString("Async");

                if (methodPath.StartsWith("/"))
                {
                    Path = methodPath;
                    Path = Path.Substring(1);
                    return;
                }

                Path = $"{contractPath}/{methodPath}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(DefaultRout)}:{DefaultRout}\r\n{nameof(Routs)}:\r\n{Routs.ListToString("\r\n")}\r\n{nameof(SwaggerRouts)}:\r\n{SwaggerRouts.ListToString("\r\n")}";
        }
    }
}