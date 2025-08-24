using SmokehouseBot.Helpers;
using SmokehouseDTOs;
using System.Text;
using TradingStrategies.Helpers.Interfaces;

namespace SmokehouseBot.Services
{
    public class TradingCalculator : ITradingCalculator
    {
        private readonly ILogger<ITradingCalculator> _logger;

        public TradingCalculator(ILogger<ITradingCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public double? CalculateRSI(List<PseudoCandlestick> pseudoCandles, int periods)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating RSI for {periods} periods.");

            if (periods < 1)
            {
                log.AppendLine($"Invalid periods parameter: {periods}. Must be at least 1.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Periods must be at least 1.", nameof(periods));
            }

            if (pseudoCandles == null || pseudoCandles.Count < periods + 1)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: {periods + 1}.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var prices = pseudoCandles.TakeLast(periods + 1).Select(pc => pc.MidClose).ToList();
            if (prices.Any(p => p < 0))
            {
                log.AppendLine("Negative prices detected.");
                _logger.LogWarning("{Log}", log.ToString());
            }

            double avgGain = 0, avgLoss = 0;
            for (int i = 1; i <= periods; i++)
            {
                double change = prices[i] - prices[i - 1];
                if (change > 0)
                {
                    avgGain += change;
                }
                else if (change < 0)
                {
                    avgLoss += Math.Abs(change);
                }
            }

            avgGain = avgGain / periods;
            avgLoss = avgLoss / periods;

            double? rsi;
            if (avgLoss == 0 && avgGain != 0)
            {
                log.AppendLine("Average loss is zero. RSI is 100.");
                rsi = 100;
            }
            else if (avgGain == 0 && avgLoss != 0)
            {
                log.AppendLine("Average gain is zero. RSI is 0.");
                rsi = 0;
            }
            else if (avgGain == 0 && avgLoss == 0)
            {
                log.AppendLine("Both average gain and loss are zero. RSI is 50.");
                rsi = 50;
            }
            else
            {
                double rs = avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
            }

            rsi = double.IsNaN(rsi.Value) || double.IsInfinity(rsi.Value) ? null : rsi;
            _logger.LogDebug("{Log}", log.ToString());

            return rsi;
        }

        public (double? MACD, double? Signal, double? Histogram) CalculateMACD(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating MACD with Short={shortPeriod}, Long={longPeriod}, Signal={signalPeriod} periods.");

            int totalPeriods = longPeriod + signalPeriod - 1;
            if (pseudoCandlesticks?.Count < totalPeriods)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandlesticks?.Count ?? 0}, Required: {totalPeriods}.");
                _logger.LogDebug("{Log}", log.ToString());
                return (null, null, null);
            }

            var prices = pseudoCandlesticks.Select(pc => pc.MidClose).ToList();
            log.AppendLine($"Price series: [{string.Join(", ", prices)}]");

            var macdValues = new List<double?>();
            for (int i = longPeriod - 1; i < prices.Count; i++)
            {
                double? shortEma = TradingCalculations.ComputeIterativeEMA(prices, shortPeriod, i);
                double? longEma = TradingCalculations.ComputeIterativeEMA(prices, longPeriod, i);
                if (shortEma.HasValue && longEma.HasValue)
                {
                    double macd = shortEma.Value - longEma.Value;
                    macdValues.Add(macd);
                    log.AppendLine($"Index {i}: ShortEMA={shortEma:F6}, LongEMA={longEma:F6}, MACD={macd:F6}");
                }
                else
                {
                    macdValues.Add(null);
                    log.AppendLine($"Index {i}: ShortEMA={(shortEma.HasValue ? shortEma.Value.ToString("F6") : "null")}, LongEMA={(longEma.HasValue ? longEma.Value.ToString("F6") : "null")}, MACD=null");
                }
            }

            double? signalLine = null;
            if (macdValues.Count > 0)
            {
                signalLine = macdValues[0].Value; // Start with first MACD
                double multiplier = 2.0 / (signalPeriod + 1);
                for (int i = 1; i < macdValues.Count; i++)
                {
                    if (macdValues[i].HasValue)
                    {
                        signalLine = (macdValues[i].Value * multiplier) + (signalLine.Value * (1 - multiplier));
                        log.AppendLine($"Signal Update i={i}: MACD={macdValues[i].Value:F6}, Signal={signalLine:F6}");
                    }
                }
            }

            double? macdLine = macdValues.Last();
            double? histogram = macdLine.HasValue && signalLine.HasValue ? macdLine - signalLine : null;
            log.AppendLine($"MACD Line: {(macdLine.HasValue ? macdLine.Value.ToString("F6") : "null")}, Signal Line: {(signalLine.HasValue ? signalLine.Value.ToString("F6") : "null")}, Histogram: {(histogram.HasValue ? histogram.Value.ToString("F6") : "null")}");
            _logger.LogDebug("{Log}", log.ToString());

            return (macdLine, signalLine, histogram);
        }


        public (double? Lower, double? Middle, double? Upper) CalculateBollingerBands(
            List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating Bollinger Bands for {period} periods with multiplier {stdDevMultiplier}");

            if (period < 1)
            {
                log.AppendLine($"Invalid period: {period}. Must be at least 1.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }
            if (stdDevMultiplier < 0)
            {
                log.AppendLine($"Invalid multiplier: {stdDevMultiplier}. Must be non-negative.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Standard deviation multiplier must be non-negative.", nameof(stdDevMultiplier));
            }

            if (pseudoCandles == null || pseudoCandles.Count < period)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: {period}.");
                _logger.LogDebug("{Log}", log.ToString());
                return (null, null, null);
            }

            var prices = pseudoCandles.TakeLast(period).Select(pc => pc.MidClose).ToList();
            double sma = prices.Average();
            if (double.IsNaN(sma) || double.IsInfinity(sma))
            {
                log.AppendLine($"Invalid SMA: {sma}.");
                _logger.LogWarning("{Log}", log.ToString());
                return (null, null, null);
            }

            double stdDev = Math.Sqrt(prices.Average(p => Math.Pow(p - sma, 2)));
            if (double.IsNaN(stdDev) || double.IsInfinity(stdDev))
            {
                log.AppendLine($"Invalid StdDev: {stdDev}.");
                _logger.LogWarning("{Log}", log.ToString());
                return (sma, null, null);
            }

            double upper = sma + stdDevMultiplier * stdDev;
            double lower = sma - stdDevMultiplier * stdDev;
            if (double.IsNaN(upper) || double.IsInfinity(upper) || double.IsNaN(lower) || double.IsInfinity(lower))
            {
                log.AppendLine($"Invalid bands: Upper={upper}, Lower={lower}.");
                _logger.LogWarning("{Log}", log.ToString());
                return (sma, null, null);
            }

            log.AppendLine($"SMA: {sma}, StdDev: {stdDev}, Upper: {upper}, Lower: {lower}");
            _logger.LogDebug("{Log}", log.ToString());
            return (lower, sma, upper);
        }


        public (double? K, double? D) CalculateStochastic(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating Stochastic for KPeriod={kPeriod}, DPeriod={dPeriod}");

            if (kPeriod < 1 || dPeriod < 1)
            {
                log.AppendLine($"Invalid periods: KPeriod={kPeriod}, DPeriod={dPeriod}.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Periods must be at least 1.");
            }

            if (pseudoCandles == null || pseudoCandles.Count < kPeriod)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: {kPeriod}.");
                _logger.LogDebug("{Log}", log.ToString());
                return (null, null);
            }

            // Calculate %K for the latest candlestick
            var prices = pseudoCandles.TakeLast(kPeriod)
                .Select(pc => (High: pc.MidHigh, Low: pc.MidLow, Close: pc.MidClose))
                .ToList();
            double highestHigh = prices.Max(p => p.High);
            double lowestLow = prices.Min(p => p.Low);
            double currentClose = prices.Last().Close;
            double k = (highestHigh - lowestLow) == 0 ? 50 : 100 * (currentClose - lowestLow) / (highestHigh - lowestLow);
            log.AppendLine($"Highest High: {highestHigh}, Lowest Low: {lowestLow}, Current Close: {currentClose}, %K: {k}");

            // Calculate %K for previous periods to compute %D
            var kValues = new List<double> { k };
            for (int offset = 1; offset < dPeriod && pseudoCandles.Count - offset >= kPeriod; offset++)
            {
                int endIndex = pseudoCandles.Count - offset - 1;
                var slice = pseudoCandles.Skip(endIndex - kPeriod + 1).Take(kPeriod)
                    .Select(pc => (High: pc.MidHigh, Low: pc.MidLow, Close: pc.MidClose))
                    .ToList();
                double hh = slice.Max(p => p.High);
                double ll = slice.Min(p => p.Low);
                double kValue = (hh - ll) == 0 ? 50 : 100 * (slice.Last().Close - ll) / (hh - ll);
                kValues.Add(kValue);
                log.AppendLine($"Offset {offset}: Using candles {endIndex - kPeriod + 1} to {endIndex}, Close={slice.Last().Close}, HH={hh}, LL={ll}, %K={kValue}");
            }

            double d = kValues.TakeLast(Math.Min(kValues.Count, dPeriod)).Average();
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                log.AppendLine($"Invalid %D: {d}.");
                _logger.LogWarning("{Log}", log.ToString());
                return (k, null);
            }

            log.AppendLine($"%D: {d}");
            _logger.LogDebug("{Log}", log.ToString());
            return (k, d);
        }


        public double? CalculateATR(List<PseudoCandlestick> pseudoCandles, int period)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating ATR for {period} periods.");

            if (period < 1)
            {
                log.AppendLine($"Invalid period: {period}. Must be at least 1.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }

            if (pseudoCandles == null || pseudoCandles.Count < period + 1)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: {period + 1}.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var trueRanges = new List<double>();
            var candles = pseudoCandles.TakeLast(period + 1).ToList();
            for (int i = 1; i < candles.Count; i++)
            {
                double high = candles[i].MidHigh;
                double low = candles[i].MidLow;
                double prevClose = candles[i - 1].MidClose;
                double tr = Math.Max(high - low, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
                if (double.IsNaN(tr) || double.IsInfinity(tr))
                {
                    log.AppendLine($"Invalid true range at index {i}: {tr}.");
                    _logger.LogWarning("{Log}", log.ToString());
                    return null;
                }
                trueRanges.Add(tr);
            }

            double result = trueRanges.Average();
            log.AppendLine($"ATR: {result}");
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                log.AppendLine($"Invalid ATR: {result}.");
                _logger.LogWarning("{Log}", log.ToString());
                return null;
            }

            _logger.LogDebug("{Log}", log.ToString());
            return result;
        }

        public decimal? CalculateVWAP(List<PseudoCandlestick> pseudoCandles, int periods)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating VWAP for {periods} periods.");

            if (periods < 1)
            {
                log.AppendLine($"Invalid period: {periods}. Must be at least 1.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Period must be at least 1.", nameof(periods));
            }

            if (pseudoCandles == null || pseudoCandles.Count < 1)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: 1.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var data = pseudoCandles.TakeLast(periods).Select(pc =>
            {
                double typical = (pc.MidHigh + pc.MidLow + pc.MidClose) / 3;
                return (Price: typical, Volume: pc.Volume);
            }).ToList();
            decimal totalVolume = data.Sum(d => d.Volume);
            if (totalVolume == 0)
            {
                log.AppendLine("Total volume is zero.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            decimal weightedSum = data.Sum(d => (decimal)d.Price * d.Volume);
            decimal result = ((decimal)weightedSum / totalVolume);
            log.AppendLine($"Weighted Sum: {weightedSum}, Total Volume: {totalVolume}, VWAP: {result}");

            _logger.LogDebug("{Log}", log.ToString());
            return result;
        }

        public decimal CalculateOBV(List<PseudoCandlestick> pseudoCandles)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating OBV for {pseudoCandles?.Count ?? 0} candlesticks.");

            if (pseudoCandles == null || pseudoCandles.Count < 2)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: 2.");
                _logger.LogDebug("{Log}", log.ToString());
                return 0;
            }

            var data = pseudoCandles.Select(pc => (Close: pc.MidClose, Volume: pc.Volume)).ToList();
            decimal obv = 0;
            for (int i = 1; i < data.Count; i++)
            {
                if (data[i].Close > data[i - 1].Close)
                    obv += data[i].Volume;
                else if (data[i].Close < data[i - 1].Close)
                    obv -= data[i].Volume;
            }

            log.AppendLine($"Final OBV: {obv}");
            _logger.LogDebug("{Log}", log.ToString());
            return obv;
        }

        /// <summary>
        /// Calculates statistically significant historical support and resistance levels for a given market ticker.
        /// 
        /// This method identifies key price levels by smoothing the frequency of price occurrences using a Gaussian filter,
        /// detecting peaks in the smoothed distribution, and ensuring a minimum distance between levels to avoid clustering.
        /// The strength of each level is calculated by combining the smoothed frequency with the total trading volume
        /// around the peak price (±1 cent). It uses the average of AskClose and BidClose prices for normalization.
        /// Price levels must have a minimum number of touches within ±1 cent of the peak, defined as a percentage of total valid candlesticks.
        /// Candlesticks with UTC timestamps between 07:00:00 and 11:59:00 (truncated to minutes) are excluded from the analysis.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol being analyzed (e.g., 'BTCUSD').</param>
        /// <param name="candlesticks">List of candlestick data points containing price, volume, and UTC timestamp information.</param>
        /// <param name="minCandlestickPercentage">Minimum percentage of total valid candlesticks required for touches within ±1 cent of a price level (default: 0.1 for 10%).</param>
        /// <param name="maxLevels">Maximum number of support/resistance levels to return (default: 6).</param>
        /// <param name="sigma">Standard deviation for Gaussian smoothing, controlling the price range for consolidation (default: 2.0 cents).</param>
        /// <param name="minDistance">Minimum price distance (in cents) between selected levels to prevent clustering (default: 3).</param>
        /// <returns>A list of <see cref="SupportResistanceLevel"/> objects, sorted by strength in descending order.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="minCandlestickPercentage"/> is less than 0 or <paramref name="maxLevels"/> is less than 1.</exception>
        public List<SupportResistanceLevel> CalculateHistoricalSupportResistance(
            string marketTicker,
            List<CandlestickData> candlesticks,
            double minCandlestickPercentage,
            int maxLevels,
            double sigma,
            int minDistance)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating Unified Historical Supports and Resistances for {marketTicker}");

            if (minCandlestickPercentage < 0)
                throw new ArgumentException("Minimum candlestick percentage must be non-negative.", nameof(minCandlestickPercentage));
            if (maxLevels < 1)
                throw new ArgumentException("Max levels must be at least 1.", nameof(maxLevels));
            if (candlesticks == null || candlesticks.Count < 3)
            {
                _logger.LogDebug("Insufficient data. Candles: {Count}, Required: 3.", candlesticks?.Count ?? 0);
                return new List<SupportResistanceLevel>();
            }

            // Filter candlesticks outside 07:00:00 to 11:59:00 UTC
            var validCandlesticks = candlesticks.Where(c =>
            {
                var truncatedTime = TradingCalculations.TruncateToMinute(c.Date.ToUniversalTime());
                var timeOfDay = truncatedTime.TimeOfDay;
                return timeOfDay < new TimeSpan(7, 0, 0) || timeOfDay > new TimeSpan(11, 59, 0);
            }).ToList();

            log.AppendLine($"Filtered out {candlesticks.Count - validCandlesticks.Count} candlesticks between 07:00:00 and 11:59:00 UTC. Valid candlesticks: {validCandlesticks.Count}");

            if (validCandlesticks.Count < 3)
            {
                _logger.LogDebug("Insufficient valid candlesticks after filtering: {Count}, Required: 3.", validCandlesticks.Count);
                return new List<SupportResistanceLevel>();
            }

            // Calculate minimum touches based on percentage of valid candlesticks
            int minTouches = Math.Max(1, (int)Math.Round(validCandlesticks.Count * minCandlestickPercentage));
            log.AppendLine($"Minimum touches required: {minTouches} ({minCandlestickPercentage:P0} of {validCandlesticks.Count} valid candlesticks)");

            // Collect frequency and volume for each normalized price (1 to 99 cents)
            var priceFrequency = new Dictionary<int, int>();
            var priceVolume = new Dictionary<int, long>();
            foreach (var candle in validCandlesticks)
            {
                int normalizedPrice = (int)Math.Round((candle.AskClose + candle.BidClose) / 2.0);
                if (normalizedPrice < 1 || normalizedPrice > 99)
                    continue;

                if (priceFrequency.ContainsKey(normalizedPrice))
                {
                    priceFrequency[normalizedPrice]++;
                    priceVolume[normalizedPrice] += candle.Volume;
                }
                else
                {
                    priceFrequency[normalizedPrice] = 1;
                    priceVolume[normalizedPrice] = candle.Volume;
                }
            }
            log.AppendLine($"Collected data for {priceFrequency.Count} unique price levels.");

            // Create frequency and volume arrays
            const int minPrice = 1;
            const int maxPrice = 99;
            var frequencyArray = new double[maxPrice - minPrice + 1];
            var volumeArray = new double[maxPrice - minPrice + 1];
            foreach (var kvp in priceFrequency)
            {
                int index = kvp.Key - minPrice;
                frequencyArray[index] = kvp.Value;
                volumeArray[index] = priceVolume[kvp.Key];
            }

            // Apply Gaussian smoothing
            var gaussianKernel = TradingCalculations.GenerateGaussianKernel(sigma);
            var smoothedFrequency = TradingCalculations.Convolve(frequencyArray, gaussianKernel);

            // Identify local maxima
            var peaks = TradingCalculations.FindLocalMaxima(smoothedFrequency);

            // Sort peaks by smoothed frequency
            var sortedPeaks = peaks.OrderByDescending(p => smoothedFrequency[p]).ToList();

            // Select top peaks with minimum distance and sufficient touches
            var selectedLevels = new List<SupportResistanceLevel>();
            var selectedPrices = new HashSet<int>();
            foreach (var peakIndex in sortedPeaks)
            {
                int price = peakIndex + minPrice;
                if (selectedPrices.Any(p => Math.Abs(p - price) < minDistance))
                    continue;

                // Check total touches within ±1 cent of the peak price
                int totalTouches = 0;
                for (int p = Math.Max(minPrice, price - 1); p <= Math.Min(maxPrice, price + 1); p++)
                {
                    int idx = p - minPrice;
                    totalTouches += (int)frequencyArray[idx];
                }
                if (totalTouches < minTouches)
                    continue;

                // Calculate strength with volume blending
                double strength = smoothedFrequency[peakIndex];
                long totalVolume = 0;
                for (int p = Math.Max(minPrice, price - 1); p <= Math.Min(maxPrice, price + 1); p++)
                {
                    int idx = p - minPrice;
                    totalVolume += (long)volumeArray[idx];
                }
                strength *= totalVolume;

                var level = new SupportResistanceLevel
                {
                    Price = price,
                    TestCount = totalTouches,
                    TotalVolume = totalVolume,
                    CandlestickCount = totalTouches,
                    Strength = Math.Round(strength, 2)
                };

                selectedLevels.Add(level);
                selectedPrices.Add(price);

                if (selectedLevels.Count >= maxLevels)
                    break;
            }

            // Sort by strength
            selectedLevels = selectedLevels.OrderByDescending(l => l.Strength).ToList();

            log.AppendLine($"Selected {selectedLevels.Count} unified levels.");
            _logger.LogDebug("{Log}", log.ToString());
            return selectedLevels;
        }

    }
}