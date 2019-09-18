namespace NetRpc.RabbitMQ
{
    public static class TypeExtensions
    {
        public static void CopyPropertiesFrom<T>(this T toObj, T fromObj)
        {
            var sourcePublicProperties = typeof(T).GetProperties();
            if (sourcePublicProperties.Length == 0)
                return;

            foreach (var sourcePublicProperty in sourcePublicProperties)
                sourcePublicProperty.SetValue(toObj, sourcePublicProperty.GetValue(fromObj, null), null);
        }
    }
}