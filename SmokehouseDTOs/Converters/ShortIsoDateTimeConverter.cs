using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.Converters
{
    // Compact ISO-8601 without fractional seconds; tolerant reader.
    public sealed class ShortIsoDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out var dt)) return dt;
            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var ticks))
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.Kind == DateTimeKind.Utc)
                writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
            else
                writer.WriteStringValue(new DateTimeOffset(value).ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture));
        }
    }
}