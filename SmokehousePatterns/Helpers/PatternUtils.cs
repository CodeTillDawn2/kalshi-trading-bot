using SmokehouseDTOs;
using static SmokehousePatterns.Helpers.TrendCalcs;
namespace SmokehousePatterns.Helpers
{
    public static class PatternUtils
    {
        public static CandleMetrics GetCandleMetrics(
            ref Dictionary<int, CandleMetrics> metricsCache,
            int index,
            CandleMids[] prices,
            int trendLookback,
            bool loadLookbackMetrics)
        {
            if (!metricsCache.TryGetValue(index, out CandleMetrics metrics))
            {
                var candle = prices[index];
                double[] meanTrend = new double[5];
                double[] lookbackAvgRange = new double[5];
                double[] trendConsistency = new double[5];
                double[] avgVoumeVsLookback = new double[5];
                double[] bullishRatio = new double[5];
                double[] bearishRatio = new double[5];
                if (loadLookbackMetrics)
                {
                    if (index >= 2) trendConsistency[0] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 1);
                    if (index >= 3) trendConsistency[1] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 2);
                    if (index >= 4) trendConsistency[2] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 3);
                    if (index >= 5) trendConsistency[3] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 4);
                    if (index >= 6) trendConsistency[4] = CalculateLookbackTrendConsistency(index, trendLookback, prices, 5);
                    if (index >= 2) lookbackAvgRange[0] = CalculateLookbackAvgRange(index, trendLookback, prices, 1);
                    if (index >= 3) lookbackAvgRange[1] = CalculateLookbackAvgRange(index, trendLookback, prices, 2);
                    if (index >= 4) lookbackAvgRange[2] = CalculateLookbackAvgRange(index, trendLookback, prices, 3);
                    if (index >= 5) lookbackAvgRange[3] = CalculateLookbackAvgRange(index, trendLookback, prices, 4);
                    if (index >= 6) lookbackAvgRange[4] = CalculateLookbackAvgRange(index, trendLookback, prices, 5);
                    if (index >= 2) meanTrend[0] = CalculateLookbackMeanTrend(prices, index, trendLookback, 1);
                    if (index >= 3) meanTrend[1] = CalculateLookbackMeanTrend(prices, index, trendLookback, 2);
                    if (index >= 4) meanTrend[2] = CalculateLookbackMeanTrend(prices, index, trendLookback, 3);
                    if (index >= 5) meanTrend[3] = CalculateLookbackMeanTrend(prices, index, trendLookback, 4);
                    if (index >= 6) meanTrend[4] = CalculateLookbackMeanTrend(prices, index, trendLookback, 5);
                    if (index >= 2) avgVoumeVsLookback[0] = CalculateAverageVolume(prices, index, trendLookback, 1);
                    if (index >= 3) avgVoumeVsLookback[1] = CalculateAverageVolume(prices, index, trendLookback, 2);
                    if (index >= 4) avgVoumeVsLookback[2] = CalculateAverageVolume(prices, index, trendLookback, 3);
                    if (index >= 5) avgVoumeVsLookback[3] = CalculateAverageVolume(prices, index, trendLookback, 4);
                    if (index >= 6) avgVoumeVsLookback[4] = CalculateAverageVolume(prices, index, trendLookback, 5);
                    if (index >= 2) bullishRatio[0] = CalculateTrendDirectionRatio(index, trendLookback, prices, 1, true);
                    if (index >= 3) bullishRatio[1] = CalculateTrendDirectionRatio(index, trendLookback, prices, 2, true);
                    if (index >= 4) bullishRatio[2] = CalculateTrendDirectionRatio(index, trendLookback, prices, 3, true);
                    if (index >= 5) bullishRatio[3] = CalculateTrendDirectionRatio(index, trendLookback, prices, 4, true);
                    if (index >= 6) bullishRatio[4] = CalculateTrendDirectionRatio(index, trendLookback, prices, 5, true);
                    if (index >= 2) bearishRatio[0] = CalculateTrendDirectionRatio(index, trendLookback, prices, 1, false);
                    if (index >= 3) bearishRatio[1] = CalculateTrendDirectionRatio(index, trendLookback, prices, 2, false);
                    if (index >= 4) bearishRatio[2] = CalculateTrendDirectionRatio(index, trendLookback, prices, 3, false);
                    if (index >= 5) bearishRatio[3] = CalculateTrendDirectionRatio(index, trendLookback, prices, 4, false);
                    if (index >= 6) bearishRatio[4] = CalculateTrendDirectionRatio(index, trendLookback, prices, 5, false);
                }

                // Calculate intervalType based on timestamp difference
                int intervalType = 0; // Default value
                if (index > 0) // Ensure there’s a previous candle to compare
                {
                    double timeDiff = (prices[index].Timestamp - prices[index - 1].Timestamp).TotalSeconds;
                    // Assuming Timestamp is in milliseconds (adjust if it's in seconds or another unit)
                    if (timeDiff == 60 * 1000) // 1 minute in milliseconds
                        intervalType = 1;
                    else if (timeDiff == 60 * 60 * 1000) // 1 hour in milliseconds
                        intervalType = 2;
                    else if (timeDiff == 24 * 60 * 60 * 1000) // 1 day in milliseconds
                        intervalType = 3;
                }

                metrics = new CandleMetrics
                {
                    BodySize = Math.Abs(candle.Close - candle.Open),
                    UpperWick = candle.High - Math.Max(candle.Open, candle.Close),
                    LowerWick = Math.Min(candle.Open, candle.Close) - candle.Low,
                    TotalRange = candle.High - candle.Low,
                    BodyToRangeRatio = candle.High != candle.Low ? Math.Abs(candle.Close - candle.Open) / (candle.High - candle.Low) : 0,
                    MidPoint = (candle.Open + candle.Close) / 2.0,
                    HasNoUpperWick = candle.High == Math.Max(candle.Open, candle.Close),
                    HasNoLowerWick = candle.Low == Math.Min(candle.Open, candle.Close),
                    BodyMidPoint = (candle.Open + candle.Close) / 2.0,
                    LookbackMeanTrend = meanTrend,
                    LookbackAvgRange = lookbackAvgRange,
                    LookbackTrendConsistency = trendConsistency,
                    AvgVoumeVsLookback = avgVoumeVsLookback,
                    BullishRatio = bullishRatio,
                    BearishRatio = bearishRatio,
                    IntervalType = intervalType,
                    IsBullish = candle.Close > candle.Open,
                    IsBearish = candle.Open > candle.Close,
                };
                metricsCache[index] = metrics;
            }

            return metrics;
        }


        public static bool IsPatternSignificant(CandleMids current, CandleMids? previous)
        {
            if (previous == null) return true; // Always process the first candle

            double currentMidpoint = current.Close;
            double previousMidpoint = previous.Close;

            return Math.Abs(currentMidpoint - previousMidpoint) >= 0.5 || // Moderate price movement
               current.High - current.Low >= 1.0 || // Significant volatility
               Math.Abs(currentMidpoint - current.Open) >= 0.5; // Notable body size
        }



        public static DateTime GetBreakpointTimestamp(string timestamp)
        {
            // Parse the exact ISO 8601 format and enforce UTC
            return DateTime.ParseExact(timestamp, "yyyy-MM-dd'T'HH:mm:ss'Z'", null, System.Globalization.DateTimeStyles.RoundtripKind);
        }


    }
}
