using System.Text.Json;
using System.Text.Json.Serialization;


namespace BacklashDTOs.Converters;

/// <summary>
/// JSON converter that preserves raw JSON strings, handling embedded JSON objects/arrays or legacy quoted strings.
/// </summary>
public sealed class RawJsonStringConverter : JsonConverter<string>
{
    /// <summary>
    /// Reads the JSON value and returns it as a raw string, preserving embedded JSON structures.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The raw JSON string or null.</returns>
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

    /// <summary>
    /// Writes the raw JSON string to the writer, preserving the JSON structure.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The raw JSON string to write.</param>
    /// <param name="options">The serialization options.</param>
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
