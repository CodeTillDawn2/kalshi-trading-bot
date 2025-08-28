using System;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace SmokehouseDTOs.Converters;
public sealed class RawJsonStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Accept embedded JSON (object/array/primitive) OR legacy quoted string.
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    return doc.RootElement.GetRawText();
                }
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Number:
            default:
                {
                    // Fallback: capture whatever is there as raw text.
                    using var doc = JsonDocument.ParseValue(ref reader);
                    return doc.RootElement.GetRawText();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            writer.WriteNullValue();
            return;
        }

        using var doc = JsonDocument.Parse(value);
        doc.RootElement.WriteTo(writer);
    }
}
