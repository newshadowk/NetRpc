using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;

namespace NetRpc.Http
{
    public sealed class HttpServiceOptions
    {
        /// <summary>
        /// Api root path, like '/api', default value is null.
        /// </summary>
        public string? ApiRootPath { get; set; }

        /// <summary>
        /// Set true will pass to next middleware when not match the method, default value is false.
        /// </summary>
        public bool IgnoreWhenNotMatched { get; set; }
    }

    public class KeyRoles
    {
        public string Key { get; set; } = null!;
        public List<string> Roles { get; set; } = new List<string>();

        public static KeyRoles ToLower(KeyRoles kr)
        {
            KeyRoles ret = new KeyRoles();
            ret.Key = kr.Key.ToLower();
            kr.Roles.ForEach(i => ret.Roles.Add(i.ToLower()));
            return ret;
        }
    }

    public sealed class SwaggerOptions
    {
        public List<KeyRoles> Items { get; set; } = new List<KeyRoles>();
    }

    public class SwaggerKeyRoles
    {
        public SwaggerKeyRoles(IOptions<SwaggerOptions> options)
        {
            var dic = new Dictionary<string, ReadOnlyCollection<string>>();
            foreach (var item in options.Value.Items.Select(KeyRoles.ToLower)) 
                dic.Add(item.Key, new ReadOnlyCollection<string>(item.Roles));
            _map = new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(dic);
        }

        private readonly IReadOnlyDictionary<string, ReadOnlyCollection<string>> _map;

        public ReadOnlyCollection<string> GetRoles(string? key)
        {
            if (key == null)
                return new ReadOnlyCollection<string>(new List<string>());
            return _map[key.ToLower()];
        }
    }
}