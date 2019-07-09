using System;

namespace NetRpc
{
    [Serializable]
    public sealed class ActionInfo
    {
        public string FullName { get; set; }

        public string[] GenericArguments { get; set; } = new string[0];

        public override string ToString()
        {
            return $"{FullName}<{GenericArguments.ListToString(",")}>";
        }
    }
}