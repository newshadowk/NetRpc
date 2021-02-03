using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetRpc
{
    public class StreamConverter : JsonConverter<Stream>
    {
        public override Stream Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return null!;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Stream dateTimeValue,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue("");
        }
    }
}