using System.Text.Json;
using System.Text.Json.Serialization;


namespace SmokehouseDTOs
{
    public class CacheSnapshot
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, MarketSnapshot> Markets { get; set; }
        public double AccountBalance { get; set; }
        public double PortfolioValue { get; set; }
        public DateTime LastWebSocketTimestamp { get; set; }
        public string SoftwareVersion { get; set; }
        public int? SnapshotVersion { get; set; }

        public CacheSnapshot()
        {
            // Parameterless constructor for deserialization
            Markets = new Dictionary<string, MarketSnapshot>();
        }

        public CacheSnapshot(DateTime snapshotDate, string softwareVersion, int? snapshotVersion, double accountBalance, double portfolioValue, DateTime lastWebSocketTimestamp, List<MarketSnapshot> marketSnapshots)
        {
            Timestamp = snapshotDate;
            Markets = new Dictionary<string, MarketSnapshot>();
            foreach (var snapshot in marketSnapshots)
            {
                Markets[snapshot.MarketTicker] = snapshot;
            }
            SoftwareVersion = softwareVersion;
            AccountBalance = accountBalance;
            PortfolioValue = portfolioValue;
            LastWebSocketTimestamp = lastWebSocketTimestamp;
            SnapshotVersion = snapshotVersion;
        }

    }

    public class SimplifiedTupleConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof((int, int, DateTime)) || typeToConvert == typeof((int, DateTime)) || typeToConvert == typeof((int, int));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof((int, int, DateTime)))
                return new Tuple_int_int_datetime_Converter();
            if (typeToConvert == typeof((int, DateTime)))
                return new Tuple_int_datetime_Converter();
            if (typeToConvert == typeof((int, int)))
                return new Tuple_int_int_Converter();
            throw new NotSupportedException();
        }

        private class Tuple_int_int_datetime_Converter : JsonConverter<(int, int, DateTime)>
        {
            public override (int, int, DateTime) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                int ask = 0, bid = 0;
                DateTime when = default;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) return (ask, bid, when);
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    string prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask": ask = reader.GetInt32(); break;
                        case "bid": bid = reader.GetInt32(); break;
                        case "when": when = DateTime.Parse(reader.GetString()); break;
                    }
                }
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, (int, int, DateTime) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("ask", value.Item1);
                writer.WriteNumber("bid", value.Item2);
                writer.WriteString("when", value.Item3.ToString("O"));
                writer.WriteEndObject();
            }
        }


        private class Tuple_int_int_Converter : JsonConverter<(int, int)>
        {
            public override (int, int) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                int ask = 0, bid = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) return (ask, bid);
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    string prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask": ask = reader.GetInt32(); break;
                        case "bid": bid = reader.GetInt32(); break;
                    }
                }
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, (int, int) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("ask", value.Item1);
                writer.WriteNumber("bid", value.Item2);
                writer.WriteEndObject();
            }
        }

        private class Tuple_int_datetime_Converter : JsonConverter<(int, DateTime)>
        {
            public override (int, DateTime) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                int ask = 0;
                DateTime when = default;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject) return (ask, when);
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    string prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask": ask = reader.GetInt32(); break;
                        case "when": when = DateTime.Parse(reader.GetString()); break;
                    }
                }
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, (int, DateTime) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("ask", value.Item1);
                writer.WriteString("when", value.Item2.ToString("O"));
                writer.WriteEndObject();
            }

        }
    }
}