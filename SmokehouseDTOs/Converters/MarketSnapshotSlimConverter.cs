using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmokehouseDTOs.Converters
{
    /// <summary>
    /// Recursively slims EVERY MarketSnapshot field for the RawJSON payload.
    /// - Write: long → short across the entire JSON tree.
    /// - Read: short → long across the entire JSON tree (back-compat).
    /// Orderbook rows remain handled by OrderbookSlimConverter.
    /// </summary>
    public sealed class MarketSnapshotSlimConverter : JsonConverter<SmokehouseDTOs.MarketSnapshot>
    {
        // Unique, <=5-char, mnemonic shorts. Avoid collisions across contexts.
        private static readonly (string Long, string Short)[] Map = new[]
        {
            // --- Core snapshot header ---
            ("Timestamp","ts"),
            ("MarketTicker","mt"),
            ("MarketCategory","mc"),
            ("MarketStatus","ms"),
            ("SnapshotSchemaVersion","v"),
            ("Orderbook","ob"),

            // --- Bests / spreads / depth ---
            ("BestYesBid","by"),
            ("BestNoBid","bn"),
            ("BestYesAsk","ay"),
            ("BestNoAsk","an"),
            ("YesSpread","ys"),
            ("NoSpread","ns"),
            ("DepthAtBestYesBid","dby"),
            ("DepthAtBestNoBid","dbn"),
            ("TopTenPercentLevelDepth_Yes","tpy"),
            ("TopTenPercentLevelDepth_No","tpn"),
            ("BidRange_Yes","bry"),
            ("BidRange_No","brn"),
            ("TotalBidContracts_Yes","tcy"),
            ("TotalBidContracts_No","tcn"),
            ("BidCountImbalance","bci"),
            ("BidVolumeImbalance","bvi"),
            ("DepthAtTop4YesBids","t4y"),
            ("DepthAtTop4NoBids","t4n"),
            ("TotalOrderbookDepth_Yes","tdy"),
            ("TotalOrderbookDepth_No","tdn"),
            ("TotalBidVolume_Yes","tvy"),
            ("TotalBidVolume_No","tvn"),
            ("YesBidCenterOfMass","ycm"),
            ("NoBidCenterOfMass","ncm"),
            ("TolerancePercentage","tol"),

            // --- Velocities & level counts ---
            ("VelocityPerMinute_Top_Yes_Bid","vty"),
            ("VelocityPerMinute_Top_No_Bid","vtn"),
            ("VelocityPerMinute_Bottom_Yes_Bid","vby"),
            ("VelocityPerMinute_Bottom_No_Bid","vbn"),
            ("LevelCount_Top_Yes_Bid","lty"),
            ("LevelCount_Top_No_Bid","ltn"),
            ("LevelCount_Bottom_Yes_Bid","lby"),
            ("LevelCount_Bottom_No_Bid","lbn"),

            // --- Order/Trade rates, volumes, counts, sizes (per-minute) ---
            ("OrderVolumePerMinute_YesBid","ovy"),
            ("OrderVolumePerMinute_NoBid","ovn"),
            ("TradeRatePerMinute_Yes","try"),
            ("TradeRatePerMinute_No","trn"),
            ("TradeVolumePerMinute_Yes","vmy"),
            ("TradeVolumePerMinute_No","vmn"),
            ("AverageTradeSize_Yes","asy"),
            ("AverageTradeSize_No","asn"),
            ("TradeCount_Yes","tcyP"),   // distinguish from TotalBidContracts_Yes (tcy)
            ("TradeCount_No","tcnP"),

            // --- "Current*" metrics (prefix c*) ---
            ("CurrentTradeRatePerMinute_Yes","ctry"),
            ("CurrentTradeRatePerMinute_No","ctrn"),
            ("CurrentTradeVolumePerMinute_Yes","cvmy"),
            ("CurrentTradeVolumePerMinute_No","cvmn"),
            ("CurrentTradeCount_Yes","ctcy"),
            ("CurrentTradeCount_No","ctcn"),
            ("CurrentOrderVolumePerMinute_YesBid","covy"),
            ("CurrentOrderVolumePerMinute_NoBid","covn"),
            ("CurrentNonTradeRelatedOrderCount_Yes","cncy"),
            ("CurrentNonTradeRelatedOrderCount_No","cncn"),
            ("CurrentAverageTradeSize_Yes","casy"),
            ("CurrentAverageTradeSize_No","casn"),

            // --- Indicators ---
            ("RSI_Short","rsiS"),
            ("RSI_Medium","rsiM"),
            ("RSI_Long","rsiL"),
            ("MACD_Medium","macdM"),
            ("MACD_Long","macdL"),
            ("EMA_Medium","emaM"),
            ("EMA_Long","emaL"),
            ("BollingerBands_Medium","bbM"),
            ("BollingerBands_Long","bbL"),
            ("ATR_Medium","atrM"),
            ("ATR_Long","atrL"),
            ("VWAP_Short","vwapS"),
            ("VWAP_Medium","vwapM"),
            ("StochasticOscillator_Short","stoS"),
            ("StochasticOscillator_Medium","stoM"),
            ("StochasticOscillator_Long","stoL"),
            ("OBV_Medium","obvM"),
            ("OBV_Long","obvL"),
            ("ADX","adx"),
            ("PSAR","psar"),

            // --- Price extremes (Yes/No) ---
            ("AllTimeHighYes_Bid","ahy"),
            ("AllTimeLowYes_Bid","aly"),
            ("AllTimeHighNo_Bid","ahn"),
            ("AllTimeLowNo_Bid","aln"),
            ("RecentHighYes_Bid","rhy"),
            ("RecentLowYes_Bid","rly"),
            ("RecentHighNo_Bid","rhn"),
            ("RecentLowNo_Bid","rln"),

            // Sub-keys inside extremes (your serializer currently uses ask/when in RawJSON)
            ("Bid","bid"),   // fallback if tuples serialize as {Bid,When}
            ("ask","a"),
            ("when","w"),
            ("When","w"),

            // --- Support/Resistance ---
            ("AllSupportResistanceLevels","srl"),
            // Row fields (unique shorts to avoid collisions)
            ("Price","prc"),
            ("TestCount","tst"),
            ("TotalVolume","tvol"),
            ("CandlestickCount","cstk"),
            ("Strength","str"),

            // --- Position & PnL ---
            ("PositionSize","ps"),
            ("MarketExposure","me"),
            ("BuyinPrice","bp"),
            ("PositionUpside","pu"),
            ("PositionDownside","pd"),
            ("TotalTraded","tt"),
            ("RestingOrders","ro"),
            ("RealizedPnl","rp"),
            ("FeesPaid","fp"),
            ("PositionROI","proi"),
            ("PositionROIAmt","proa"),
            ("ExpectedFees","ef"),

            // --- Time/context ---
            ("ChangeMetricsMature","cmm"),
            ("MarketAge","ma"),
            ("TimeLeft","tl"),
            ("CanCloseEarly","ce"),
            ("LastWebSocketMessageReceived","lws"),
            ("MarketBehaviorYes","mby"),
            ("MarketBehaviorNo","mbn"),
            ("HoldTime","ht"),

            // --- Last 10 pseudo-candles ---
            ("LastTenCandlesticks","lc10"),
            ("Timestamp","cts"),   // candle timestamp (avoid clash with top-level ts)
            ("MidClose","mcl"),
            ("MidHigh","mhi"),
            ("MidLow","mlo"),
            ("Volume","vol"),
            ("IsFromCandlestick","ifc"),
            ("BestNoBidD","bnbd"),
            ("BestYesBidD","bybd"),
        };

        // Fast lookup dictionaries
        private static readonly Dictionary<string, string> LongToShort = Map.ToDictionary(x => x.Long, x => x.Short, StringComparer.Ordinal);
        private static readonly Dictionary<string, string> ShortToLong = Map.ToDictionary(x => x.Short, x => x.Long, StringComparer.Ordinal);

        public override SmokehouseDTOs.MarketSnapshot Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms))
            {
                RewriteObject(doc.RootElement, w, expand: true);
            }
            var normalized = Encoding.UTF8.GetString(ms.ToArray());
            var safe = CloneWithoutSelf(options);
            return JsonSerializer.Deserialize<SmokehouseDTOs.MarketSnapshot>(normalized, safe)!;
        }

        public override void Write(Utf8JsonWriter writer, SmokehouseDTOs.MarketSnapshot value, JsonSerializerOptions options)
        {
            var safe = CloneWithoutSelf(options);
            var json = JsonSerializer.Serialize(value, safe); // full MarketSnapshot JSON
            using var doc = JsonDocument.Parse(json);
            RewriteObject(doc.RootElement, writer, expand: false);
        }

        // Recursively rewrite property names using the maps.
        private static void RewriteObject(JsonElement element, Utf8JsonWriter w, bool expand)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    w.WriteStartObject();
                    foreach (var prop in element.EnumerateObject())
                    {
                        var name = prop.Name;
                        var mapped = expand ? Expand(name) : Shrink(name);
                        w.WritePropertyName(mapped);
                        RewriteObject(prop.Value, w, expand);
                    }
                    w.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    w.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                        RewriteObject(item, w, expand);
                    w.WriteEndArray();
                    break;

                default:
                    element.WriteTo(w);
                    break;
            }
        }

        private static string Expand(string name)
        {
            return ShortToLong.TryGetValue(name, out var longName) ? longName : name;
        }

        private static string Shrink(string name)
        {
            return LongToShort.TryGetValue(name, out var shortName) ? shortName : name;
        }

        private static JsonSerializerOptions CloneWithoutSelf(JsonSerializerOptions options)
        {
            var clone = new JsonSerializerOptions(options);
            for (int i = clone.Converters.Count - 1; i >= 0; i--)
                if (clone.Converters[i] is MarketSnapshotSlimConverter)
                    clone.Converters.RemoveAt(i);
            return clone;
        }
    }
}
