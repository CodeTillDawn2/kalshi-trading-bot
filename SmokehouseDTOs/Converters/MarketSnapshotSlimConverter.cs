using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.Converters
{
    // Shorten top-level keys; accept both on read.
    // Map: Timestamp->ts, MarketTicker->mt, MarketCategory->mc, MarketStatus->ms, SnapshotSchemaVersion->v, Orderbook->ob
    public sealed class MarketSnapshotSlimConverter : JsonConverter<SmokehouseDTOs.MarketSnapshot>
    {
        private static readonly (string Long, string Short)[] Map = new[]
        {
            ("Timestamp", "ts"),
            ("MarketTicker", "mt"),
            ("MarketCategory", "mc"),
            ("MarketStatus", "ms"),
            ("SnapshotSchemaVersion", "v"),
            ("Orderbook", "ob"),
        };

        public override SmokehouseDTOs.MarketSnapshot Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                w.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    w.WritePropertyName(Expand(prop.Name));
                    prop.Value.WriteTo(w);
                }
                w.WriteEndObject();
            }
            var normalized = Encoding.UTF8.GetString(ms.ToArray());
            var safe = CloneWithoutSelf(options);
            return JsonSerializer.Deserialize<SmokehouseDTOs.MarketSnapshot>(normalized, safe)!;
        }

        public override void Write(Utf8JsonWriter writer, SmokehouseDTOs.MarketSnapshot value, JsonSerializerOptions options)
        {
            var safe = CloneWithoutSelf(options);
            var json = JsonSerializer.Serialize(value, safe);
            using var doc = JsonDocument.Parse(json);
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                writer.WritePropertyName(Shrink(prop.Name));
                prop.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        private static JsonSerializerOptions CloneWithoutSelf(JsonSerializerOptions options)
        {
            var clone = new JsonSerializerOptions(options);
            for (int i = clone.Converters.Count - 1; i >= 0; i--)
                if (clone.Converters[i] is MarketSnapshotSlimConverter)
                    clone.Converters.RemoveAt(i);
            return clone;
        }

        private static string Expand(string name)
        {
            foreach (var kv in Map) if (name == kv.Short) return kv.Long;
            return name;
        }

        private static string Shrink(string name)
        {
            foreach (var kv in Map) if (name == kv.Long) return kv.Short;
            return name;
        }
    }
}