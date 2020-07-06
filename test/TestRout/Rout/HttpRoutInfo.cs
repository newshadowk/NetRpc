using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetRpc
{
    public sealed class HttpRoutInfo
    {
        private readonly List<string> _params = new List<string>();

        /// <summary>
        /// S/Get/C/\w+/sss
        /// </summary>
        private readonly string _regPatternPath;

        public bool IsPath(string paramName)
        {
            return _params.Exists(i => i == paramName);
        }

        public Dictionary<string, string> MatchesPathValues(string rawPath)
        {
            //S/Get/C/{p1}/D/{p2} defPath
            //S/Get/C/v1/D/v2  rawPath =>
            //key:p1  value v1
            //key:p2  value v2

            //S/Get/C/{p1}/D/{p2} =>
            //S/Get/C/\{p1}/D/\{p2}
            var tmpP = Regex.Escape(Path);

            var keys = new List<string>();
            var mc = Regex.Matches(tmpP, @"\\{\w+}");
            if (mc.Count == 0)
                return new Dictionary<string, string>();

            foreach (Match o in mc)
                keys.Add(o.Value.Substring(2, o.Value.Length - 3));

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
        /// S/Get/C/{p1}/sss
        /// </summary>
        public string Path { get; }

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
                return Regex.IsMatch(path, _regPatternPath);
            
            return HttpMethods.Any(i => i == method) && Regex.IsMatch(path, _regPatternPath);
        }

        public HttpRoutInfo(string contractTag, string path)
        {
            ContractTag = contractTag;
            Path = path;
            _regPatternPath = ReplacePathStr(path);

            var c = Regex.Matches(path, @"(?<={)\w+(?=})");
            foreach (Match o in c)
                _params.Add(o.Value);
        }

        public override string ToString()
        {
            return $"{ContractTag}, {Path}, {HttpMethods.ListToString(", ")}";
        }
    }
}