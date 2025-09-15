using System.Text.Json;
using System.Text.Json.Serialization;


namespace BacklashDTOs
{
    /// <summary>
    /// Represents a snapshot of the trading system's state at a specific point in time.
    /// </summary>
    public class CacheSnapshot
    {
        /// <summary>
        /// Gets or sets the timestamp when this snapshot was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of market snapshots keyed by market ticker.
        /// </summary>
        public Dictionary<string, MarketSnapshot> Markets { get; set; }

        /// <summary>
        /// Gets or sets the account balance at the time of the snapshot.
        /// </summary>
        public double AccountBalance { get; set; }

        /// <summary>
        /// Gets or sets the portfolio value at the time of the snapshot.
        /// </summary>
        public double PortfolioValue { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last WebSocket message received.
        /// </summary>
        public DateTime LastWebSocketTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the software version used to create this snapshot.
        /// </summary>
        public string? SoftwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the version number of this snapshot.
        /// </summary>
        public int? SnapshotVersion { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheSnapshot"/> class for deserialization.
        /// </summary>
        public CacheSnapshot()
        {
            // Parameterless constructor for deserialization
            Markets = new Dictionary<string, MarketSnapshot>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheSnapshot"/> class with the specified parameters.
        /// </summary>
        /// <param name="snapshotDate">The date and time of the snapshot.</param>
        /// <param name="softwareVersion">The software version.</param>
        /// <param name="snapshotVersion">The snapshot version number.</param>
        /// <param name="accountBalance">The account balance.</param>
        /// <param name="portfolioValue">The portfolio value.</param>
        /// <param name="lastWebSocketTimestamp">The timestamp of the last WebSocket message.</param>
        /// <param name="marketSnapshots">The list of market snapshots.</param>
        public CacheSnapshot(DateTime snapshotDate, string softwareVersion, int? snapshotVersion, double accountBalance, double portfolioValue, DateTime lastWebSocketTimestamp, List<MarketSnapshot> marketSnapshots)
        {
            Timestamp = snapshotDate;
            Markets = new Dictionary<string, MarketSnapshot>();
            foreach (var snapshot in marketSnapshots)
            {
                Markets[snapshot.MarketTicker ?? string.Empty] = snapshot;
            }
            SoftwareVersion = softwareVersion;
            AccountBalance = accountBalance;
            PortfolioValue = portfolioValue;
            LastWebSocketTimestamp = lastWebSocketTimestamp;
            SnapshotVersion = snapshotVersion;
        }

    }

    /// <summary>
    /// A JSON converter factory for simplified tuple types used in the application.
    /// </summary>
    public class SimplifiedTupleConverter : JsonConverterFactory
    {
        /// <summary>
        /// Determines whether the converter can convert the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>true if the converter can convert the type; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof((int, int, DateTime)) || typeToConvert == typeof((int, DateTime)) || typeToConvert == typeof((int, int));
        }

        /// <summary>
        /// Creates a converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A JSON converter for the specified type.</returns>
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
                    string? prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask":
                        case "Item1": ask = reader.GetInt32(); break;
                        case "bid":
                        case "Item2": bid = reader.GetInt32(); break;
                        case "when":
                        case "Item3":
                            string? s = reader.GetString();
                            if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) when = dt;
                            break;
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
                    string? prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask":
                        case "Item1": ask = reader.GetInt32(); break;
                        case "bid":
                        case "Item2": bid = reader.GetInt32(); break;
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
                    string? prop = reader.GetString();
                    reader.Read();
                    switch (prop)
                    {
                        case "ask":
                        case "Bid": ask = reader.GetInt32(); break;
                        case "w":
                        case "When":
                            string? s = reader.GetString();
                            if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) when = dt;
                            break;
                    }
                }
                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, (int, DateTime) value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("ask", value.Item1);
                writer.WriteString("w", value.Item2.ToString("O"));
                writer.WriteEndObject();
            }

        }
    }
}
