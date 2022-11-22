using System.Text;
using System.Text.Json;

namespace NetRpc.Http.Client;

public sealed class JsonRestfulNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var chars = name.AsSpan();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i == 0)
            {
                sb.Append(char.ToLower(chars[i]));
                continue;
            }

            if (char.IsLower(chars[i]))
            {
                sb.Append(chars[i]);
                continue;
            }

            sb.Append("_");
            sb.Append(char.ToLower(chars[i]));
        }
        return sb.ToString();
    }
}