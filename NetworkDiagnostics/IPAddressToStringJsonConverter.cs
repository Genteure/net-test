using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetworkDiagnostics
{
    public class IPAddressToStringJsonConverter : JsonConverter<IPAddress>
    {
        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var ipString = reader.GetString();
            return ipString is null ? null : IPAddress.Parse(ipString);
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
