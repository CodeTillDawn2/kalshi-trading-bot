using SmokehouseDTOs;
using SmokehousePatterns;
using System.Data.SqlClient;
using System.Text;

public class MarketProcessor
{
    private readonly string _connectionString;

    public MarketProcessor(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<CandlestickData> RetrieveCandlesticksFromDatabase(int intervalType, DateTime? startTime = null, DateTime? endTime = null, List<string>? marketTickers = null, int? topX = null, int offset = 0, bool InactiveOnly = true)
    {
        var candlesticks = new List<CandlestickData>();

        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var sql = new StringBuilder(@"
            SELECT 
                c.market_ticker, c.interval_type, c.end_period_ts, c.year, c.month, c.day, c.hour, c.minute,
                c.open_interest, c.volume, c.yes_ask_close AS ask_close, c.yes_ask_high AS ask_high,
                c.yes_ask_low AS ask_low, c.yes_ask_open AS ask_open, c.yes_bid_close AS bid_close,
                c.yes_bid_high AS bid_high, c.yes_bid_low AS bid_low, c.yes_bid_open AS bid_open
            FROM [dbo].[t_Candlesticks] c");

            string marketFilter = $"m.LastCandlestick IS NOT NULL {(InactiveOnly == true ? "AND m.Status != 'Active'" : "")}";
            string tickersClause = marketTickers != null && marketTickers.Any()
                ? $"c.market_ticker IN ({string.Join(",", marketTickers.Select((t, i) => $"@ticker{i}"))})"
                : "";

            if (topX.HasValue && topX > 0)
            {
                sql.Append($@"
                INNER JOIN (
                    SELECT market_ticker
                    FROM [dbo].[t_Markets] m
                    WHERE {marketFilter}" +
                        (marketTickers != null && marketTickers.Any() ? $" AND {tickersClause}" : "") + @"
                    ORDER BY LastCandlestick DESC
                    OFFSET @offset ROWS FETCH NEXT @topX ROWS ONLY
                ) top_markets ON c.market_ticker = top_markets.market_ticker");
            }
            else
            {
                sql.Append($@" 
                INNER JOIN [dbo].[t_Markets] m 
                ON c.market_ticker = m.market_ticker");
            }

            // Start WHERE clause for the main query
            sql.Append(" WHERE c.interval_type = @intervalType");
            if (startTime != null && endTime != null)
            {
                sql.Append(" AND c.end_period_ts BETWEEN @start AND @end");
            }
            if (marketTickers != null && marketTickers.Any() && !(topX.HasValue && topX > 0))
            {
                sql.Append($" AND {tickersClause}");
            }
            if (!(topX.HasValue && topX > 0))
            {
                sql.Append($" AND {marketFilter}");
            }

            sql.Append(" ORDER BY c.market_ticker, c.end_period_ts");

            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddWithValue("@intervalType", intervalType);
                if (startTime != null && endTime != null)
                {
                    cmd.Parameters.AddWithValue("@start", ((DateTimeOffset)startTime.Value).ToUnixTimeSeconds());
                    cmd.Parameters.AddWithValue("@end", ((DateTimeOffset)endTime.Value).ToUnixTimeSeconds());
                }
                if (marketTickers != null && marketTickers.Any())
                {
                    for (int i = 0; i < marketTickers.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@ticker{i}", marketTickers[i].Trim());
                    }
                }
                if (topX.HasValue && topX > 0)
                {
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@topX", topX.Value);
                }

                cmd.CommandTimeout = 1200;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = new DateTime(
                            reader.GetInt32(3), // year
                            reader.GetInt32(4), // month
                            reader.GetInt32(5), // day
                            reader.GetInt32(6), // hour
                            reader.GetInt32(7), // minute
                            0,
                            DateTimeKind.Utc);
                        candlesticks.Add(new CandlestickData
                        {
                            MarketTicker = reader.GetString(0),
                            IntervalType = reader.GetInt32(1),
                            Date = date,
                            OpenInterest = reader.GetInt32(8),
                            Volume = reader.GetInt32(9),
                            AskClose = reader.GetInt32(10),
                            AskHigh = reader.GetInt32(11),
                            AskLow = reader.GetInt32(12),
                            AskOpen = reader.GetInt32(13),
                            BidClose = reader.GetInt32(14),
                            BidHigh = reader.GetInt32(15),
                            BidLow = reader.GetInt32(16),
                            BidOpen = reader.GetInt32(17)
                        });
                    }
                }
            }
        }
        return candlesticks;
    }

    public List<string> RetrieveUniqueMarketsFromDatabase(DateTime? startTime = null, DateTime? endTime = null, List<string>? marketTickers = null, int? topX = null, int offset = 0)
    {
        var markets = new List<string>();

        using (var conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var sql = new StringBuilder(@"
            SELECT DISTINCT c.market_ticker
            FROM [dbo].[t_Candlesticks] c");

            string marketFilter = "m.LastCandlestick IS NOT NULL AND m.Status != 'Active'";
            string tickersClause = marketTickers != null && marketTickers.Any()
                ? $"c.market_ticker IN ({string.Join(",", marketTickers.Select((t, i) => $"@ticker{i}"))})"
                : "";

            if (topX.HasValue && topX > 0)
            {
                sql.Append($@"
                INNER JOIN (
                    SELECT market_ticker
                    FROM [dbo].[t_Markets] m
                    WHERE {marketFilter}" +
                        (marketTickers != null && marketTickers.Any() ? $" AND {tickersClause}" : "") + @"
                    ORDER BY LastCandlestick DESC
                    OFFSET @offset ROWS FETCH NEXT @topX ROWS ONLY
                ) top_markets ON c.market_ticker = top_markets.market_ticker");
            }
            else
            {
                sql.Append($@" 
                INNER JOIN [dbo].[t_Markets] m 
                ON c.market_ticker = m.market_ticker");
            }

            // Start WHERE clause for the main query
            if (startTime != null && endTime != null)
            {
                sql.Append(" WHERE c.end_period_ts BETWEEN @start AND @end");
                if (marketTickers != null && marketTickers.Any() && !(topX.HasValue && topX > 0))
                {
                    sql.Append($" AND {tickersClause}");
                }
                if (!(topX.HasValue && topX > 0))
                {
                    sql.Append($" AND {marketFilter}");
                }
            }
            else if (marketTickers != null && marketTickers.Any() && !(topX.HasValue && topX > 0))
            {
                sql.Append($" WHERE {tickersClause}");
                if (!(topX.HasValue && topX > 0))
                {
                    sql.Append($" AND {marketFilter}");
                }
            }
            else if (!(topX.HasValue && topX > 0))
            {
                sql.Append($" WHERE {marketFilter}");
            }

            sql.Append(" ORDER BY c.market_ticker");

            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                if (startTime != null && endTime != null)
                {
                    cmd.Parameters.AddWithValue("@start", ((DateTimeOffset)startTime.Value).ToUnixTimeSeconds());
                    cmd.Parameters.AddWithValue("@end", ((DateTimeOffset)endTime.Value).ToUnixTimeSeconds());
                }
                if (marketTickers != null && marketTickers.Any())
                {
                    for (int i = 0; i < marketTickers.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@ticker{i}", marketTickers[i].Trim());
                    }
                }
                if (topX.HasValue && topX > 0)
                {
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@topX", topX.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        markets.Add(reader.GetString(0));
                    }
                }
            }
        }
        return markets;
    }

    public List<CandlestickData> ForwardFillCandlesticks(List<CandlestickData> candlesticks, string marketTicker)
    {
        if (!candlesticks.Any()) return new List<CandlestickData>();

        var sortedCandles = candlesticks
            .Where(c => c.MarketTicker == marketTicker)
            .OrderBy(c => c.Date)
            .ToList();

        if (!sortedCandles.Any()) return new List<CandlestickData>();

        var intervalType = sortedCandles.First().IntervalType;
        var start = sortedCandles.First().Date;
        var end = sortedCandles.Last().Date;

        List<DateTime> allTimes;
        switch (intervalType)
        {
            case 1: // Minute
                allTimes = Enumerable.Range(0, (int)(end - start).TotalMinutes + 2)
                    .Select(m => start.AddMinutes(m))
                    .ToList();
                break;
            case 2: // Hour
                allTimes = Enumerable.Range(0, (int)(end - start).TotalHours + 2)
                    .Select(h => start.AddHours(h))
                    .ToList();
                break;
            case 3: // Day
                allTimes = Enumerable.Range(0, (end - start).Days + 2)
                    .Select(d => start.AddDays(d))
                    .ToList();
                break;
            default:
                throw new ArgumentException($"Unsupported interval type: {intervalType}");
        }

        var result = new List<CandlestickData>();
        CandlestickData lastCandle = null;

        foreach (var time in allTimes)
        {
            CandlestickData? candle;

            //Day data can sometimes be at a different hour, possibly because of daylight savings
            if (intervalType == 3)
            {
                candle = sortedCandles.FirstOrDefault(c => c.Date.ToShortDateString() == time.ToShortDateString());
            }
            else
            {
                candle = sortedCandles.FirstOrDefault(c => c.Date == time);
            }

            if (candle != null)
            {
                lastCandle = candle;
            }
            else if (lastCandle != null)
            {
                candle = new CandlestickData
                {
                    MarketTicker = marketTicker,
                    IntervalType = intervalType,
                    Date = time,
                    OpenInterest = lastCandle.OpenInterest,
                    Volume = 0,
                    AskOpen = lastCandle.AskClose,
                    AskHigh = lastCandle.AskClose,
                    AskLow = lastCandle.AskClose,
                    AskClose = lastCandle.AskClose,
                    BidOpen = lastCandle.BidClose,
                    BidHigh = lastCandle.BidClose,
                    BidLow = lastCandle.BidClose,
                    BidClose = lastCandle.BidClose
                };
            }

            if (candle != null)
            {
                result.Add(candle);
            }
        }

        return result;
    }

    public List<MarketState> ComputeMarketStates(string marketTicker, List<CandlestickData> candlesticks, int lookback = 30)
    {
        var states = new List<MarketState>();
        var filledCandles = ForwardFillCandlesticks(candlesticks, marketTicker).ToList();
        if (!filledCandles.Any()) return states;

        var intervalType = filledCandles.First().IntervalType;
        int lookbackUnits = lookback; // Number of units (minutes, hours, days)
        var lookbackQueue = new Queue<CandlestickData>(lookbackUnits + 1);

        foreach (var candle in filledCandles)
        {
            // Add current candle to queue
            lookbackQueue.Enqueue(candle);

            // Remove candles outside lookback period based on interval type
            while (lookbackQueue.Count > 0)
            {
                var oldest = lookbackQueue.Peek();
                bool isOutsideLookback = intervalType switch
                {
                    1 => (candle.Date - oldest.Date).TotalMinutes > lookbackUnits, // Minutes
                    2 => (candle.Date - oldest.Date).TotalHours > lookbackUnits,  // Hours
                    3 => (candle.Date - oldest.Date).Days > lookbackUnits,        // Days
                    _ => throw new ArgumentException($"Unsupported interval type: {intervalType}")
                };

                if (isOutsideLookback)
                    lookbackQueue.Dequeue();
                else
                    break;
            }

            var state = new MarketState
            {
                MarketTicker = marketTicker,
                Timestamp = candle.Date,
                OpenInterest = candle.OpenInterest,
                AskOpen = candle.AskOpen,
                AskHigh = candle.AskHigh,
                AskLow = candle.AskLow,
                AskClose = candle.AskClose,
                BidOpen = candle.BidOpen,
                BidHigh = candle.BidHigh,
                BidLow = candle.BidLow,
                BidClose = candle.BidClose,
                Volume = candle.Volume,
                Hour = candle.Date.Hour,
                Minute = intervalType == 1 ? candle.Date.Minute : 0, // Only set Minute for minute interval
                Day = candle.Date.Day
            };

            // Spread and MidPrice (clamped to 0–100)
            state.Spread = candle.BidHigh - candle.BidLow;

            // Support/Resistance using MidPrice, clamped to 0–100
            var midHigh = (candle.AskHigh + candle.BidHigh) / 2.0;
            var midLow = (candle.AskLow + candle.BidLow) / 2.0;
            var midClose = (candle.AskClose + candle.BidClose) / 2.0;
            var pivot = (midHigh + midLow + midClose) / 3.0;
            state.PivotPoint = pivot;
            state.Support1 = (2 * pivot) - midHigh;
            state.Support2 = pivot - (midHigh - midLow);
            state.Support3 = midLow - 2 * (midHigh - pivot);
            state.Resistance1 = (2 * pivot) - midLow;
            state.Resistance2 = (midHigh - midLow);
            state.Resistance3 = midHigh + 2 * (pivot - midLow);

            // Volume Support/Resistance
            double currentMidpoint = (state.AskClose + state.BidClose) / 2;
            state.VolumeSupport = (currentMidpoint <= Math.Max(Math.Max(state.Support1, state.Support2), state.Support3)) ? (double)candle.Volume : null;
            state.VolumeResistance = (currentMidpoint >= Math.Min(Math.Min(state.Resistance1, state.Resistance2), state.Resistance3)) ? (double)candle.Volume : null;

            // Technical Indicators using MidPrice
            var midPrices = lookbackQueue.Select(c => Math.Min(100, Math.Max(0, (double)((c.AskHigh + c.BidLow) / 2.0)))).ToArray();
            state.SMA5 = midPrices.Length >= 5 ? Math.Min(100, Math.Max(0, midPrices.TakeLast(5).Average())) : null;
            state.SMA10 = midPrices.Length >= 10 ? Math.Min(100, Math.Max(0, midPrices.TakeLast(10).Average())) : null;
            state.ATR = midPrices.Length >= 14 ? Math.Min(100, Math.Max(0, (lookbackQueue.TakeLast(14).Max(c => (double)((c.AskHigh + c.BidLow) / 2.0)) - lookbackQueue.TakeLast(14).Min(c => (double)((c.AskHigh + c.BidLow) / 2.0))))) : null;
            state.RSI = midPrices.Length >= 14 ? CalculateRSI(midPrices.TakeLast(14).ToArray()) : null;

            if (midPrices.Length >= 26)
            {
                var (macd, signal) = CalculateMACD(midPrices);
                state.MACD = Math.Min(100, Math.Max(0, macd));
                state.MACDSignal = Math.Min(100, Math.Max(0, signal));
            }
            else
            {
                state.MACD = 0;
                state.MACDSignal = 0;
            }

            states.Add(state);
        }

        return states;
    }

    private double? CalculateRSI(double[] closes)
    {
        if (closes.Length < 14) return null;

        var deltas = new double[closes.Length - 1];
        for (int i = 1; i < closes.Length; i++) deltas[i - 1] = closes[i] - closes[i - 1];

        var gains = deltas.Where(d => d > 0).ToList();
        var losses = deltas.Where(d => d < 0).Select(d => -d).ToList();

        if (!gains.Any() || !losses.Any()) return null;

        var gainsAvg = gains.Average();
        var lossesAvg = losses.Average();
        var rs = gainsAvg / lossesAvg;
        return 100 - (100 / (1 + rs));
    }

    private (double macd, double signal) CalculateMACD(double[] closes)
    {
        var ema12 = CalculateEMA(closes.TakeLast(12).ToArray(), 12);
        var ema26 = CalculateEMA(closes, 26);
        var macd = ema12 - ema26;
        var signal = closes.Length >= 35 ? CalculateEMA(new double[] { macd }.Concat(Enumerable.Repeat(0.0, 8)).ToArray(), 9) : macd; // Simplified
        return (macd, signal);
    }

    private double CalculateEMA(double[] data, int period)
    {
        double alpha = 2.0 / (period + 1);
        double ema = data[0];
        for (int i = 1; i < data.Length; i++) ema = alpha * data[i] + (1 - alpha) * ema;
        return ema;
    }
}