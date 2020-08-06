using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NetRpc.Contract;

namespace NetRpc
{
    public sealed class HttpRoutInfo
    {
        private readonly string _regPatternPathWithoutQuery;

        public ReadOnlyCollection<string> PathParams { get; }

        public bool IsPath(string paramName)
        {
            return PathParams.Any(i => i == paramName);
        }

        public Dictionary<string, string> MatchesPathValues(string rawPath)
        {
            //S/Get/C/{p1}/D/{p2} defPath
            //S/Get/C/v1/D/v2  rawPath =>
            //key:p1  value v1
            //key:p2  value v2

            //S/Get/C/{p1}/D/{p2} =>
            //S/Get/C/\{p1}/D/\{p2}
            var tmpP = Regex.Escape(PathWithoutQuery);

            var keys = new List<string>();
            var mc = Regex.Matches(tmpP, @"\\{\w+}");
            if (mc.Count == 0)
                return new Dictionary<string, string>();

            foreach (Match? o in mc)
                keys.Add(o!.Value.Substring(2, o.Value.Length - 3));

            //S/Get/C/\{p1}/D/\{p2} =>
            //S/Get/C/(\w+)/D/(\w+)
            tmpP = Regex.Replace(tmpP, @"\\{\w+}", @"(\w+)");

            //S/Get/C/v1/D/v2 matches
            //S/Get/C/(\w+)/D/(\w+)
            var dic = new Dictionary<string, string>();
            mc = Regex.Matches(rawPath, tmpP);
            var gc = mc[0].Groups;
            for (var i = 1; i < gc.Count; i++)
                dic.Add(keys[i - 1], gc[i].Value);

            return dic;
        }

        /// <summary>
        /// if null, match all.
        /// </summary>
        public IList<string> HttpMethods { get; } = new List<string>();

        public string ContractTag { get; }

        /// <summary>
        /// S/Get/C/{p1}/sss?vp2={p2}
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// ?vp2={P2}, key:vp2, value:P2
        /// </summary>
        public Dictionary<string, string> QueryParams { get; }

        /// <summary>
        /// S/Get/C/{p1}/ss
        /// </summary>
        public string PathWithoutQuery { get; }

        /// <summary>
        /// S/Get/C/{p1}/sss => S/Get/C/\w+/sss$
        /// </summary>
        private static string ReplacePathStr(string path)
        {
            //S/Get/C/{p1}/sss =>
            //S/Get/C/\{p1}/sss
            var temps = Regex.Escape(path);

            //S/Get/C/\w+/sss
            var ret = Regex.Replace(temps, @"\\{\w+}", @"\w+");
            return ret + "$";
        }

        public bool IsMatch(string path, string method)
        {
            if (HttpMethods.Count == 0)
                return Regex.IsMatch(path, _regPatternPathWithoutQuery);

            return HttpMethods.Any(i => i == method) && Regex.IsMatch(path, _regPatternPathWithoutQuery);
        }

        public HttpRoutInfo(string contractTag, string path)
        {
            ContractTag = contractTag;
            Path = path;

            string? pathQuery;
            var idx = path.IndexOf('?');
            if (idx != -1)
            {
                PathWithoutQuery = path.Substring(0, idx);
                pathQuery = path.Substring(idx);
            }
            else
            {
                PathWithoutQuery = path;
                pathQuery = null;
            }

            QueryParams = GetQueryParams(pathQuery);

            _regPatternPathWithoutQuery = ReplacePathStr(PathWithoutQuery);
            
            var ps = new List<string>();
            var c = Regex.Matches(path, @"(?<={)\w+(?=})");
            foreach (Match? o in c)
                ps.Add(o!.Value);

            PathParams = new ReadOnlyCollection<string>(ps);
        }

        private static Dictionary<string, string> GetQueryParams(string? pathQuery)
        {
            //pathQuery null
            if (string.IsNullOrWhiteSpace(pathQuery))
                return new Dictionary<string, string>();

            //pathQuery ?
            if (pathQuery!.Length == 1)
                return new Dictionary<string, string>();

            //pathQuery ?vp2={P2}&vp3={P3}
            pathQuery = pathQuery.Substring(1);
            var ret = new Dictionary<string, string>();
            try
            {
                var ss = pathQuery.Split('&');
                foreach (var s in ss)
                {
                    //vp2={P2}
                    var pair = s.Split('=');
                    //pair[0]:vp2
                    //pair[1]:{P2}
                    ret.Add(pair[0], pair[1].Substring(1, pair[1].Length - 2));
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException($"{pathQuery}", e);
            }

            return ret;
        }

        public override string ToString()
        {
            return $"{ContractTag}, {Path}, {HttpMethods.ListToString(", ")}";
        }
    }

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
            DefaultRout = GetDefaultRout(list);
            SwaggerRouts = new ReadOnlyCollection<HttpRoutInfo>(GetSwaggerRouts(list));
        }

        private static HttpRoutInfo GetDefaultRout(IList<HttpRoutInfo> list)
        {
            var post = list.FirstOrDefault(i => i.HttpMethods.Any(j => j == "POST"));
            if (post != null)
                return post;
            return list[0];
        }

        private static List<HttpRoutInfo> GetSwaggerRouts(List<HttpRoutInfo> list)
        {
            var ris = list.FindAll(i => i.HttpMethods.Count > 0);
            if (ris.Count == 0)
            {
                var ri = new HttpRoutInfo(list[0].ContractTag, list[0].Path);
                ri.HttpMethods.Add("POST");
                ris.Add(ri);
            }
            return ris;
        }

        private static List<HttpRouteAttribute> GetRoutAttributes(Type contractType)
        {
            var contractRoutes = contractType.GetCustomAttributes<HttpRouteAttribute>(true).ToList();
            if (contractRoutes.Any())
                return contractRoutes.ToList();

            contractRoutes.Add(new HttpRouteAttribute(contractType.Name));
            return contractRoutes;
        }

        private static List<HttpRoutInfo> GetRouts(Type contractType, MethodInfo methodInfo)
        {
            var contractTrimAsync = contractType.IsDefined(typeof(HttpTrimAsyncAttribute));
            var methodTrimAsync = methodInfo.IsDefined(typeof(HttpTrimAsyncAttribute)) || contractTrimAsync;

            var contractRoutes = GetRoutAttributes(contractType);
            var tag = contractType.GetCustomAttribute<TagAttribute>(true);
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

                //add default
                if (methodHttpMethods.Count == 0 && methodRoutes.Count == 0)
                {
                    tempInfos.Add(new TempInfo(
                        contractType,
                        methodInfo,
                        r.Template,
                        null,
                        null,
                        contractTrimAsync,
                        methodTrimAsync
                    ));
                }
            }

            //add default
            if (tempInfos.Count == 0)
            {
                tempInfos.Add(new TempInfo(
                    contractType,
                    methodInfo,
                    null,
                    null,
                    null,
                    contractTrimAsync,
                    methodTrimAsync
                ));
            }

            //merger to result.
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

        private static string GetTag(TagAttribute? tag, Type contractType, bool contractTrimAsync)
        {
            string contractPath;
            //contractPath
            if (tag == null || string.IsNullOrWhiteSpace(tag.Name))
                contractPath = contractType.Name;
            else
                contractPath = tag.Name;

            if (contractTrimAsync)
                contractPath = contractPath.TrimEndString("Async")!;
            return contractPath;
        }

        private class TempInfo
        {
            public string? Method { get; }
            public string Path { get; }

            public TempInfo(Type contractType, MethodInfo methodInfo, string? contractTemplate, string? methodTemplate, string? method, bool contractTrimAsync, 
                bool methodTrimAsync)
            {
                Method = method;

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
                    contractPath = contractPath.TrimEndString("Async")!;

                string methodPath;
                if (methodTemplate == null)
                    methodPath = methodInfo.Name;
                else
                    methodPath = methodTemplate;

                if (methodTrimAsync)
                    methodPath = methodPath.TrimEndString("Async")!;

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