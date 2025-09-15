using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Converters
{
    /// <summary>
    /// JSON converter for DateTime that uses compact ISO-8601 format without fractional seconds and has a tolerant reader.
    /// </summary>
    public sealed class ShortIsoDateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Reads the JSON value and converts it to a DateTime, supporting string and number formats.
        /// </summary>
        /// <param name="reader">The UTF-8 JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>The parsed DateTime.</returns>
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

        /// <summary>
        /// Writes the DateTime value to JSON in compact ISO-8601 format.
        /// </summary>
        /// <param name="writer">The UTF-8 JSON writer.</param>
        /// <param name="value">The DateTime value to write.</param>
        /// <param name="options">The serialization options.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.Kind == DateTimeKind.Utc)
                writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
            else
                writer.WriteStringValue(new DateTimeOffset(value).ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture));
        }
    }
}
