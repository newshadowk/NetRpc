using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NetRpc.Http.Client
{
    public class DtoContractResolver : CamelCasePropertyNamesContractResolver
    {
        public static readonly DtoContractResolver Instance = new DtoContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType.HasStream()) 
                property.ShouldSerialize = instance => false;
            return property;
        }
    }
}