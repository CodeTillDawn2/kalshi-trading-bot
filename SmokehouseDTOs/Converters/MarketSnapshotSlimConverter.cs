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
            // Distinguish from TotalBidContracts_Yes (tcy) / _No (tcn)
            ("TradeCount_Yes","tcyP"),
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
            ("PlusDI","pdi"),
            ("MinusDI","mdi"),
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

            // Sub-keys inside extremes (tuples serialize as {Bid,When})
            ("Bid","a"),      // map tuple 'Bid' → short 'a' (price)
            ("When","w"),     // map tuple 'When' → short 'w' (timestamp)

            // --- Support/Resistance ---
            ("AllSupportResistanceLevels","srl"),
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
            ("GoodBadPriceYes","gby"),
            ("GoodBadPriceNo","gbn"),
            ("HoldTime","ht"),

            // --- Highest/Recent volumes ---
            ("HighestVolume_Day","hvd"),
            ("HighestVolume_Hour","hvh"),
            ("HighestVolume_Minute","hvm"),
            ("RecentVolume_LastHour","rvlh"),
            ("RecentVolume_LastThreeHours","rvl3"),
            ("RecentVolume_LastMonth","rvlm"),

            // --- Bollinger band subkeys ---
            ("lower","lo"),
            ("middle","mid"),
            ("upper","up"),

            // --- MACD subkeys ---
            ("Signal","sig"),
            ("Histogram","hist"),

            // --- Last 10 pseudo-candles ---
            ("RecentCandlesticks","rc"),
            ("MidClose","mcl"),
            ("MidHigh","mhi"),
            ("MidLow","mlo"),
            ("Volume","vol"),
            ("IsFromCandlestick","ifc"),

            // Dollars versions of best bids (from your model)
            ("BestYesBidD","bybd"),
            ("BestNoBidD","bnbd"),
            ("NoBidSlopePerMinute_Short","nbsls"),
            ("YesBidSlopePerMinute_Short","ybsls"),
            ("YesBidSlopePerMinute_Medium","ybslm"),
            ("NoBidSlopePerMinute_Medium","nbslm"),
            ("NonTradeRelatedOrderCount_Yes","ytroc"),
            ("NonTradeRelatedOrderCount_No","ntroc"),
        };

        // Lookup dictionaries (collision-safe for ShortToLong)
        private static readonly Dictionary<string, string> LongToShort =
            Map.ToDictionary(x => x.Long, x => x.Short, StringComparer.Ordinal);

        private static readonly Dictionary<string, string> ShortToLong = BuildShortToLong();

        private static Dictionary<string, string> BuildShortToLong()
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (Long, Short) in Map)
            {
                if (!d.ContainsKey(Short))
                {
                    d[Short] = Long; // first wins
                }
            }
            return d;
        }

        public override SmokehouseDTOs.MarketSnapshot Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
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
        private static void RewriteObject(JsonElement element, Utf8JsonWriter w, bool expand, List<string>? path = null)
        {
            path ??= new List<string>(8);

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    w.WriteStartObject();
                    foreach (var prop in element.EnumerateObject())
                    {
                        string mapped;
                        if (expand)
                        {
                            // short -> long
                            if (prop.Name == "cts" && IsInCandles(path))
                                mapped = "Timestamp";
                            else
                                mapped = Expand(prop.Name);
                        }
                        else
                        {
                            // long -> short
                            if (prop.Name == "Timestamp" && IsInCandles(path))
                                mapped = "cts";
                            else
                                mapped = Shrink(prop.Name);
                        }

                        w.WritePropertyName(mapped);

                        // descend with updated path (use mapped so IsInCandles sees either long or short)
                        path.Add(mapped);
                        RewriteObject(prop.Value, w, expand, path);
                        path.RemoveAt(path.Count - 1);
                    }
                    w.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    w.WriteStartArray();
                    // mark array level in path so ancestor check still works
                    path.Add("[]");
                    foreach (var item in element.EnumerateArray())
                        RewriteObject(item, w, expand, path);
                    path.RemoveAt(path.Count - 1);
                    w.WriteEndArray();
                    break;

                default:
                    element.WriteTo(w);
                    break;
            }
        }

        private static bool IsInCandles(IReadOnlyList<string> path)
        {
            // Accept either long or short ancestor name
            for (int i = path.Count - 1; i >= 0; i--)
                if (path[i] is "lc10" or "LastTenCandlesticks")
                    return true;
            return false;
        }

        private static string Expand(string name)
        {
            // Expand short -> long; handle special-case if needed
            if (name == "w") return "When";
            if (name == "a") return "Bid";
            return ShortToLong.TryGetValue(name, out var longName) ? longName : name;
        }

        private static string Shrink(string name)
        {
            // Accept both casings for tuple member names
            if (name is "When" or "when") return "w";
            if (name is "Bid" or "bid") return "a";
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