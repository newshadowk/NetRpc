using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;

namespace NetRpc.Http;

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

    public string? ShortConnRedisConnStr { get; set; }

    public string? ShortConnTempDir { get; set; }
}

public class KeyRole
{
    public string Key { get; set; } = null!;

    public string Role { get; set; } = null!;
}

public sealed class SwaggerOptions
{
    public List<KeyRole> Items { get; set; } = new();
}

public class SwaggerKeyRoles
{
    public SwaggerKeyRoles(IOptions<SwaggerOptions> options)
    {
        var dic = new Dictionary<string, ReadOnlyCollection<string>>();
        foreach (var i in options.Value.Items)
            dic.Add(i.Key.ToLower(), new ReadOnlyCollection<string>(SplitRole(i.Role)));
        _map = new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(dic);
    }

    private readonly IReadOnlyDictionary<string, ReadOnlyCollection<string>> _map;

    public ReadOnlyCollection<string> GetRoles(string? key)
    {
        if (key == null)
            return new ReadOnlyCollection<string>(new List<string> {"default"});

        if (_map.TryGetValue(key.ToLower(), out var values))
            return values;
        return new ReadOnlyCollection<string>(new List<string>());
    }

    private static List<string> SplitRole(string role)
    {
        var ret = new List<string>();
        var ss = role.ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries);
        foreach (var s in ss)
        {
            var s1 = s.Trim();
            ret.Add(s1);
        }

        return ret;
    }
}

public class DocXmlOptions
{
    public List<string> Paths { get; set; } = new();
}