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

        /// <summary>
        /// Calculates the Parabolic Stop and Reverse (PSAR) value for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator is valuable for event-driven contracts where price trends can emerge rapidly due to evolving news or data releases influencing outcome probabilities.
        /// It identifies trend directions and provides dynamic trailing stop levels, aiding risk management in volatile, bounded-price environments (1 to 99 cents per contract).
        /// However, in ranging or sideways markets—common when events are distant or uncertain—PSAR may generate frequent false reversal signals (whipsaws), potentially leading to unnecessary trades.
        /// It performs best when combined with trend-confirming indicators like the Average Directional Index (ADX) to filter signals.
        /// </summary>
        /// <param name="candlesticks">The list of candlesticks containing ask and bid prices for high, low, and close.</param>
        /// <param name="initialAF">The initial acceleration factor (default: 0.02).</param>
        /// <param name="maxAF">The maximum acceleration factor (default: 0.2).</param>
        /// <param name="afStep">The step increment for the acceleration factor (default: 0.02).</param>
        /// <returns>The current PSAR value, or null if insufficient data or invalid computation.</returns>
        /// <remarks>
        /// Different PSAR values indicate the following in Kalshi's context:
        /// - A PSAR value below the current price (e.g., PSAR at 45 when the mid-close is 60) suggests an uptrend, signaling potential buying opportunities or holding long ("Yes") positions, with the PSAR serving as a rising trailing stop.
        /// - A PSAR value above the current price (e.g., PSAR at 75 when the mid-close is 60) indicates a downtrend, implying selling pressure or short ("No") positions, with the PSAR acting as a falling trailing stop.
        /// - Crossovers, where the price moves beyond the PSAR, denote potential trend reversals: a price rising above PSAR flips to bullish, while falling below flips to bearish.
        /// - Acceleration factor (AF) increases (up to the maximum, typically 0.20) amplify the PSAR's sensitivity in strong trends, tightening stops to lock in gains.
        /// </remarks>
        public double? CalculatePSAR(List<CandlestickData> candlesticks, double initialAF = 0.02, double maxAF = 0.2, double afStep = 0.02)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating PSAR with InitialAF={initialAF}, MaxAF={maxAF}, AFStep={afStep}.");

            if (initialAF <= 0 || maxAF <= 0 || afStep <= 0 || initialAF > maxAF)
            {
                log.AppendLine("Invalid acceleration parameters.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Acceleration factors must be positive and InitialAF <= MaxAF.");
            }

            if (candlesticks == null || candlesticks.Count < 2)
            {
                log.AppendLine($"Insufficient data. Candles: {candlesticks?.Count ?? 0}, Required: 2.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var highs = candlesticks.Select(c => (c.AskHigh + c.BidHigh) / 2.0).ToList();
            var lows = candlesticks.Select(c => (c.AskLow + c.BidLow) / 2.0).ToList();
            var closes = candlesticks.Select(c => (c.AskClose + c.BidClose) / 2.0).ToList();

            double psar = lows[0]; // Initialize with first low
            int trend = closes[1] > closes[0] ? 1 : -1; // 1 for uptrend, -1 for downtrend
            double ep = trend == 1 ? highs[0] : lows[0]; // Extreme point
            double af = initialAF;

            for (int i = 1; i < candlesticks.Count; i++)
            {
                double currentPsar = psar + af * (ep - psar);

                if (trend == 1) // Uptrend
                {
                    if (lows[i] < currentPsar) // Reversal to downtrend
                    {
                        trend = -1;
                        psar = i > 0 ? Math.Max(highs[i - 1], highs[i]) : highs[i];
                        ep = lows[i];
                        af = initialAF;
                    }
                    else
                    {
                        psar = currentPsar;
                        if (highs[i] > ep)
                        {
                            ep = highs[i];
                            af = Math.Min(af + afStep, maxAF);
                        }
                    }
                }
                else // Downtrend
                {
                    if (highs[i] > currentPsar) // Reversal to uptrend
                    {
                        trend = 1;
                        psar = i > 0 ? Math.Min(lows[i - 1], lows[i]) : lows[i];
                        ep = highs[i];
                        af = initialAF;
                    }
                    else
                    {
                        psar = currentPsar;
                        if (lows[i] < ep)
                        {
                            ep = lows[i];
                            af = Math.Min(af + afStep, maxAF);
                        }
                    }
                }

                log.AppendLine($"Index {i}: PSAR={psar:F4}, Trend={(trend == 1 ? "Up" : "Down")}, EP={ep:F4}, AF={af:F4}");
            }

            if (double.IsNaN(psar) || double.IsInfinity(psar))
            {
                log.AppendLine($"Invalid PSAR: {psar}.");
                _logger.LogWarning("{Log}", log.ToString());
                return null;
            }

            _logger.LogDebug("{Log}", log.ToString());
            return psar;
        }

        /// <summary>
        /// Calculates the Average Directional Index (ADX) value for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator serves as a robust complementary metric, particularly when integrated with tools like the Parabolic Stop and Reverse (PSAR) to validate trend strength.
        /// It provides an objective measure of trend intensity, irrespective of direction, which aids in distinguishing genuine trends from periods of consolidation or volatility without momentum.
        /// This is especially pertinent in Kalshi's event-driven environment, where contract prices may exhibit pronounced trends due to evolving probabilities influenced by economic announcements or other developments.
        /// By confirming the presence of a strong trend (typically ADX values exceeding 25), it enhances the efficacy of PSAR signals, reducing the likelihood of whipsaws in non-trending conditions.
        /// Nonetheless, ADX functions as a lagging indicator and does not inherently specify trend direction; it necessitates pairing with directional components like the Positive Directional Indicator (+DI) and Negative Directional Indicator (-DI), or external tools such as PSAR.
        /// Its utility diminishes in highly volatile, range-bound markets common to certain Kalshi contracts awaiting resolution.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing mid-high, mid-low, and mid-close prices.</param>
        /// <param name="period">The period for ADX calculation (default: 14).</param>
        /// <returns>The current ADX value, or null if insufficient data or invalid computation.</returns>
        /// <remarks>
        /// Interpretations of ADX values are as follows in Kalshi's context:
        /// - Below 20: Indicates a weak or absent trend, suggesting consolidation or choppy conditions; traders should exercise caution with trend-following strategies like PSAR, as signals may prove unreliable.
        /// - Between 20 and 25: Signals the potential emergence of a trend; this transitional range warrants monitoring for confirmation, such as a PSAR crossover aligned with rising ADX.
        /// - Between 25 and 40: Reflects a strong trend, validating PSAR positions and encouraging trend continuation trades, with higher values denoting increased momentum.
        /// - Above 40: Denotes an exceptionally strong trend, often nearing potential exhaustion; while supportive of PSAR trailing stops, it may foreshadow reversals, prompting consideration of profit-taking.
        /// </remarks>
        public double? CalculateADX(List<PseudoCandlestick> pseudoCandles, int period = 14)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating ADX for {period} periods.");

            if (period < 1)
            {
                log.AppendLine($"Invalid period: {period}. Must be at least 1.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Period must be at least 1.", nameof(period));
            }

            if (pseudoCandles == null || pseudoCandles.Count < period * 2)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: {period * 2}.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var highs = pseudoCandles.Select(pc => pc.MidHigh).ToList();
            var lows = pseudoCandles.Select(pc => pc.MidLow).ToList();
            var closes = pseudoCandles.Select(pc => pc.MidClose).ToList();

            // Calculate smoothed +DM, -DM, and TR
            double smoothedPlusDM = 0, smoothedMinusDM = 0, smoothedTR = 0;
            for (int i = 1; i <= period; i++)
            {
                double upMove = highs[i] - highs[i - 1];
                double downMove = lows[i - 1] - lows[i];
                double plusDM = (upMove > downMove && upMove > 0) ? upMove : 0;
                double minusDM = (downMove > upMove && downMove > 0) ? downMove : 0;
                double tr = Math.Max(highs[i] - lows[i], Math.Max(Math.Abs(highs[i] - closes[i - 1]), Math.Abs(lows[i] - closes[i - 1])));
                smoothedPlusDM += plusDM;
                smoothedMinusDM += minusDM;
                smoothedTR += tr;
            }
            smoothedPlusDM /= period;
            smoothedMinusDM /= period;
            smoothedTR /= period;

            // Calculate subsequent values iteratively
            var dxValues = new List<double>();
            for (int i = period + 1; i < pseudoCandles.Count; i++)
            {
                double upMove = highs[i] - highs[i - 1];
                double downMove = lows[i - 1] - lows[i];
                double plusDM = (upMove > downMove && upMove > 0) ? upMove : 0;
                double minusDM = (downMove > upMove && downMove > 0) ? downMove : 0;
                double tr = Math.Max(highs[i] - lows[i], Math.Max(Math.Abs(highs[i] - closes[i - 1]), Math.Abs(lows[i] - closes[i - 1])));

                smoothedPlusDM = ((smoothedPlusDM * (period - 1)) + plusDM) / period;
                smoothedMinusDM = ((smoothedMinusDM * (period - 1)) + minusDM) / period;
                smoothedTR = ((smoothedTR * (period - 1)) + tr) / period;

                if (smoothedTR == 0) continue; // Avoid division by zero

                double plusDI = (smoothedPlusDM / smoothedTR) * 100;
                double minusDI = (smoothedMinusDM / smoothedTR) * 100;
                double dx = Math.Abs(plusDI - minusDI) / (plusDI + minusDI) * 100;
                dxValues.Add(dx);

                log.AppendLine($"Index {i}: +DM={plusDM:F4}, -DM={minusDM:F4}, TR={tr:F4}, +DI={plusDI:F2}, -DI={minusDI:F2}, DX={dx:F2}");
            }

            if (dxValues.Count < period)
            {
                log.AppendLine($"Insufficient DX values for ADX: {dxValues.Count}, Required: {period}.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            // Calculate ADX as SMA of last 'period' DX values
            double adx = dxValues.TakeLast(period).Average();
            if (double.IsNaN(adx) || double.IsInfinity(adx))
            {
                log.AppendLine($"Invalid ADX: {adx}.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            log.AppendLine($"ADX: {adx:F2}");
            _logger.LogDebug("{Log}", log.ToString());
            return adx;
        }
    }
}