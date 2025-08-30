using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.Converters
{
    // Slims rows: price->p, side y/n->s, resting_contracts->q, last_modified_date->t
    public sealed class OrderbookSlimConverter : JsonConverter<List<Dictionary<string, object>>>
    {
        public override List<Dictionary<string, object>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<Dictionary<string, object>>();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray) return list;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    using var _ = JsonDocument.ParseValue(ref reader);
                    continue;
                }

                int? price = null, qty = null;
                string? side = null;
                DateTime? when = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        using var _ = JsonDocument.ParseValue(ref reader);
                        continue;
                    }
                    var name = reader.GetString();
                    reader.Read();

                    switch (name)
                    {
                        case "p":
                        case "price":
                            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var p)) price = p;
                            else using (var _ = JsonDocument.ParseValue(ref reader)) { }
                            break;
                        case "s":
                        case "side":
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                var v = reader.GetString();
                                side = v == "y" ? "yes" : v == "n" ? "no" :
                                       (v?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true ? "yes" :
                                        v?.Equals("no", StringComparison.OrdinalIgnoreCase) == true ? "no" : v);
                            }
                            else using (var _ = JsonDocument.ParseValue(ref reader)) { }
                            break;
                        case "q":
                        case "resting_contracts":
                            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var q)) qty = q;
                            else using (var _ = JsonDocument.ParseValue(ref reader)) { }
                            break;
                        case "t":
                        case "last_modified_date":
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                var s = reader.GetString();
                                if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) when = dt;
                            }
                            else using (var _ = JsonDocument.ParseValue(ref reader)) { }
                            break;
                        default:
                            using (var _ = JsonDocument.ParseValue(ref reader)) { }
                            break;
                    }
                }

                var row = new Dictionary<string, object>(4);
                if (price.HasValue) row["price"] = price.Value;
                if (!string.IsNullOrEmpty(side)) row["side"] = side!;
                if (qty.HasValue) row["resting_contracts"] = qty.Value;
                if (when.HasValue) row["last_modified_date"] = when.Value;
                list.Add(row);
            }

            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<Dictionary<string, object>> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var row in value)
            {
                row.TryGetValue("price", out var p);
                row.TryGetValue("side", out var s);
                row.TryGetValue("resting_contracts", out var q);
                row.TryGetValue("last_modified_date", out var t);

                writer.WriteStartObject();
                if (p is IConvertible) writer.WriteNumber("p", Convert.ToInt32(p));
                if (s is string ss) writer.WriteString("s", ss.Equals("yes", StringComparison.OrdinalIgnoreCase) ? "y" : ss.Equals("no", StringComparison.OrdinalIgnoreCase) ? "n" : ss);
                if (q is IConvertible) writer.WriteNumber("q", Convert.ToInt32(q));
                if (t is DateTime dt)
                {
                    writer.WriteString("t", dt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}