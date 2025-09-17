using BacklashBot.Helpers;
using BacklashDTOs;
using System.Text;
using BacklashBot.State;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services
{
    /// <summary>
    /// Provides calculations for various technical indicators used in trading strategies for the Kalshi marketplace.
    /// This class implements the ITradingCalculator interface and computes indicators such as RSI, MACD, Bollinger Bands,
    /// Stochastic Oscillator, ATR, VWAP, OBV, support/resistance levels, PSAR, and ADX based on pseudo-candlestick data.
    /// All calculations are designed for event-driven contracts with prices bounded between 1 and 99 cents.
    /// Depends on CalculationConfig for indicator parameters, includes async methods for performance in high-frequency scenarios,
    /// and incorporates comprehensive input validation including null checks, empty list handling, and data integrity checks with warnings.
    /// </summary>
    /// <remarks>
    /// Indicators are calculated using standard formulas adapted for Kalshi's binary outcome markets.
    /// Null values are returned when insufficient data is available for reliable calculations.
    /// Input validation ensures data quality by checking for null lists, empty collections, and invalid candlestick data (e.g., negative prices, invalid timestamps).
    /// </remarks>
    public class TradingCalculator : ITradingCalculator
    {
        private readonly ILogger<ITradingCalculator> _logger;
        private readonly CalculationConfig _config;

        /// <summary>
        /// Initializes a new instance of the TradingCalculator class with the specified logger and configuration.
        /// The CalculationConfig provides parameters for indicator calculations such as PSAR acceleration factors, ADX periods, and exponential multipliers.
        /// </summary>
        /// <param name="logger">The logger instance for recording calculation events and errors.</param>
        /// <param name="config">The CalculationConfig instance containing parameters for technical indicator calculations.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger or config is null.</exception>
        public TradingCalculator(ILogger<ITradingCalculator> logger, CalculationConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates the Relative Strength Index (RSI) for a series of pseudo-candlesticks.
        /// RSI measures the speed and change of price movements on a scale of 0 to 100.
        /// Values above 70 indicate overbought conditions, below 30 indicate oversold conditions.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="periods">The number of periods to use for the RSI calculation (typically 14).</param>
        /// <returns>The RSI value (0-100) or null if insufficient data or invalid calculation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when periods is less than 1.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateRSIAsync(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public double? CalculateRSI(List<PseudoCandlestick> pseudoCandles, int periods)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for RSI calculation.");
                        return null;
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
                    if (periods < 1)
                    {
                        _logger.LogError("Invalid periods parameter: {Periods}. Must be at least 1.", periods);
                        throw new ArgumentException("Periods must be at least 1.", nameof(periods));
                    }

            if (pseudoCandles == null || pseudoCandles.Count < periods + 1)
            {
                _logger.LogDebug("Insufficient data for RSI calculation. Candles: {Count}, Required: {Required}.",
                    pseudoCandles?.Count ?? 0, periods + 1);
                return null;
            }

            var prices = pseudoCandles.TakeLast(periods + 1).Select(pc => pc.MidClose).ToList();
            if (prices.Any(p => p < 0))
            {
                _logger.LogWarning("Negative prices detected in RSI calculation for market data.");
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

            avgGain /= periods;
            avgLoss /= periods;

            double? rsi;
            if (avgLoss == 0 && avgGain != 0)
            {
                rsi = 100;
            }
            else if (avgGain == 0 && avgLoss != 0)
            {
                rsi = 0;
            }
            else if (avgGain == 0 && avgLoss == 0)
            {
                rsi = 50;
            }
            else
            {
                double rs = avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
            }

            rsi = double.IsNaN(rsi.Value) || double.IsInfinity(rsi.Value) ? null : rsi;
            _logger.LogDebug("RSI calculated: {RSI} for {Periods} periods.", rsi, periods);

            return rsi;
        }

        /// <summary>
        /// Calculates the Relative Strength Index (RSI) asynchronously for a series of pseudo-candlesticks.
        /// RSI measures the speed and change of price movements on a scale of 0 to 100.
        /// Values above 70 indicate overbought conditions, below 30 indicate oversold conditions.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="periods">The number of periods to use for the RSI calculation (typically 14).</param>
        /// <returns>A task that represents the asynchronous operation, containing the RSI value (0-100) or null if insufficient data or invalid calculation.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateRSI(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public async Task<double?> CalculateRSIAsync(List<PseudoCandlestick> pseudoCandles, int periods)
        {
            return await Task.Run(() => CalculateRSI(pseudoCandles, periods));
        }

        /// <summary>
        /// Calculates the Moving Average Convergence Divergence (MACD) indicator.
        /// MACD shows the relationship between two moving averages of a security's price.
        /// The MACD line is the difference between short and long EMAs, signal line is EMA of MACD,
        /// and histogram shows the difference between MACD and signal lines.
        /// </summary>
        /// <param name="pseudoCandlesticks">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="shortPeriod">The period for the short EMA (typically 12).</param>
        /// <param name="longPeriod">The period for the long EMA (typically 26).</param>
        /// <param name="signalPeriod">The period for the signal line EMA (typically 9).</param>
        /// <returns>A tuple containing MACD line, signal line, and histogram values, or nulls if insufficient data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandlesticks is null.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateMACDAsync(List{PseudoCandlestick}, int, int, int)"/>
        /// </remarks>
        public (double? MACD, double? Signal, double? Histogram) CalculateMACD(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod)
                {
                    if (pseudoCandlesticks == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandlesticks), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandlesticks.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for MACD calculation.");
                        return (null, null, null);
                    }
                    for (int i = 0; i < pseudoCandlesticks.Count; i++)
                    {
                        var pc = pseudoCandlesticks[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandlesticks[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
                    int totalPeriods = longPeriod + signalPeriod - 1;
                    if (pseudoCandlesticks?.Count < totalPeriods)
                    {
                        _logger.LogDebug("Insufficient data for MACD calculation. Candles: {Count}, Required: {Required}.",
                            pseudoCandlesticks?.Count ?? 0, totalPeriods);
                        return (null, null, null);
                    }

            var prices = pseudoCandlesticks.Select(pc => pc.MidClose).ToList();

            var macdValues = new List<double?>();
            for (int i = longPeriod - 1; i < prices.Count; i++)
            {
                double? shortEma = TradingCalculations.CalculateIterativeEMA(prices, shortPeriod, i);
                double? longEma = TradingCalculations.CalculateIterativeEMA(prices, longPeriod, i);
                if (shortEma.HasValue && longEma.HasValue)
                {
                    double macd = shortEma.Value - longEma.Value;
                    macdValues.Add(macd);
                }
                else
                {
                    macdValues.Add(null);
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
                    }
                }
            }

            double? macdLine = macdValues.Last();
            double? histogram = macdLine.HasValue && signalLine.HasValue ? macdLine - signalLine : null;
            _logger.LogDebug("MACD calculated: Line={Line}, Signal={Signal}, Histogram={Histogram}.",
                macdLine, signalLine, histogram);

            return (macdLine, signalLine, histogram);
        }

        /// <summary>
        /// Calculates the Moving Average Convergence Divergence (MACD) indicator asynchronously.
        /// MACD shows the relationship between two moving averages of a security's price.
        /// The MACD line is the difference between short and long EMAs, signal line is EMA of MACD,
        /// and histogram shows the difference between MACD and signal lines.
        /// </summary>
        /// <param name="pseudoCandlesticks">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="shortPeriod">The period for the short EMA (typically 12).</param>
        /// <param name="longPeriod">The period for the long EMA (typically 26).</param>
        /// <param name="signalPeriod">The period for the signal line EMA (typically 9).</param>
        /// <returns>A task that represents the asynchronous operation, containing a tuple of MACD line, signal line, and histogram values, or nulls if insufficient data.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateMACD(List{PseudoCandlestick}, int, int, int)"/>
        /// </remarks>
        public async Task<(double? MACD, double? Signal, double? Histogram)> CalculateMACDAsync(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod)
        {
            return await Task.Run(() => CalculateMACD(pseudoCandlesticks, shortPeriod, longPeriod, signalPeriod));
        }

        /// <summary>
        /// Calculates Bollinger Bands, which consist of a middle band (SMA) and upper/lower bands based on standard deviation.
        /// Bollinger Bands are used to identify volatility and potential price reversals.
        /// Price touching the upper band may indicate overbought conditions, lower band oversold.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="period">The period for the moving average and standard deviation calculation (typically 20).</param>
        /// <param name="stdDevMultiplier">The multiplier for standard deviation to determine band width (typically 2.0).</param>
        /// <returns>A tuple containing lower band, middle band (SMA), and upper band values, or nulls if insufficient data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when period is less than 1 or multiplier is negative.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateBollingerBandsAsync(List{PseudoCandlestick}, int, double)"/>
        /// </remarks>
        public (double? Lower, double? Middle, double? Upper) CalculateBollingerBands(
                    List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for Bollinger Bands calculation.");
                        return (null, null, null);
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
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

        /// <summary>
        /// Calculates Bollinger Bands asynchronously, which consist of a middle band (SMA) and upper/lower bands based on standard deviation.
        /// Bollinger Bands are used to identify volatility and potential price reversals.
        /// Price touching the upper band may indicate overbought conditions, lower band oversold.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="period">The period for the moving average and standard deviation calculation (typically 20).</param>
        /// <param name="stdDevMultiplier">The multiplier for standard deviation to determine band width (typically 2.0).</param>
        /// <returns>A task that represents the asynchronous operation, containing a tuple of lower band, middle band (SMA), and upper band values, or nulls if insufficient data.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateBollingerBands(List{PseudoCandlestick}, int, double)"/>
        /// </remarks>
        public async Task<(double? Lower, double? Middle, double? Upper)> CalculateBollingerBandsAsync(
            List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier)
        {
            return await Task.Run(() => CalculateBollingerBands(pseudoCandles, period, stdDevMultiplier));
        }

        /// <summary>
        /// Calculates the Stochastic Oscillator, which compares a closing price to its price range over a period.
        /// %K shows the current close relative to the high-low range, %D is the moving average of %K.
        /// Values above 80 indicate overbought, below 20 indicate oversold.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="kPeriod">The period for %K calculation (typically 14).</param>
        /// <param name="dPeriod">The period for %D moving average (typically 3).</param>
        /// <returns>A tuple containing %K and %D values (0-100), or nulls if insufficient data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when periods are less than 1.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateStochasticAsync(List{PseudoCandlestick}, int, int)"/>
        /// </remarks>
        public (double? K, double? D) CalculateStochastic(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for Stochastic calculation.");
                        return (null, null);
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
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

        /// <summary>
        /// Calculates the Stochastic Oscillator asynchronously, which compares a closing price to its price range over a period.
        /// %K shows the current close relative to the high-low range, %D is the moving average of %K.
        /// Values above 80 indicate overbought, below 20 indicate oversold.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="kPeriod">The period for %K calculation (typically 14).</param>
        /// <param name="dPeriod">The period for %D moving average (typically 3).</param>
        /// <returns>A task that represents the asynchronous operation, containing a tuple of %K and %D values (0-100), or nulls if insufficient data.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateStochastic(List{PseudoCandlestick}, int, int)"/>
        /// </remarks>
        public async Task<(double? K, double? D)> CalculateStochasticAsync(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod)
        {
            return await Task.Run(() => CalculateStochastic(pseudoCandles, kPeriod, dPeriod));
        }

        /// <summary>
        /// Calculates the Average True Range (ATR), which measures volatility by averaging the true range over a period.
        /// True range is the maximum of: current high - current low, |current high - previous close|, |current low - previous close|.
        /// Higher ATR values indicate higher volatility.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="period">The period for averaging the true range (typically 14).</param>
        /// <returns>The ATR value or null if insufficient data or invalid calculation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when period is less than 1.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// Parameters are sourced from configuration where applicable (e.g., ADX period).
        /// <see cref="CalculateATRAsync(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public double? CalculateATR(List<PseudoCandlestick> pseudoCandles, int period)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for ATR calculation.");
                        return null;
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
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

        /// <summary>
        /// Calculates the Average True Range (ATR) asynchronously, which measures volatility by averaging the true range over a period.
        /// True range is the maximum of: current high - current low, |current high - previous close|, |current low - previous close|.
        /// Higher ATR values indicate higher volatility.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price data.</param>
        /// <param name="period">The period for averaging the true range (typically 14).</param>
        /// <returns>A task that represents the asynchronous operation, containing the ATR value or null if insufficient data or invalid calculation.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateATR(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public async Task<double?> CalculateATRAsync(List<PseudoCandlestick> pseudoCandles, int period)
        {
            return await Task.Run(() => CalculateATR(pseudoCandles, period));
        }

        /// <summary>
        /// Calculates the Volume Weighted Average Price (VWAP) over a specified number of periods.
        /// VWAP is the average price weighted by volume, providing insight into the average price paid by market participants.
        /// It's commonly used as a benchmark for intraday trading.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price and volume data.</param>
        /// <param name="periods">The number of periods to include in the VWAP calculation.</param>
        /// <returns>The VWAP value or null if insufficient data or zero volume.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when periods is less than 1.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateVWAPAsync(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public decimal? CalculateVWAP(List<PseudoCandlestick> pseudoCandles, int periods)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for VWAP calculation.");
                        return null;
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
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

        /// <summary>
        /// Calculates the Volume Weighted Average Price (VWAP) asynchronously over a specified number of periods.
        /// VWAP is the average price weighted by volume, providing insight into the average price paid by market participants.
        /// It's commonly used as a benchmark for intraday trading.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price and volume data.</param>
        /// <param name="periods">The number of periods to include in the VWAP calculation.</param>
        /// <returns>A task that represents the asynchronous operation, containing the VWAP value or null if insufficient data or zero volume.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateVWAP(List{PseudoCandlestick}, int)"/>
        /// </remarks>
        public async Task<decimal?> CalculateVWAPAsync(List<PseudoCandlestick> pseudoCandles, int periods)
        {
            return await Task.Run(() => CalculateVWAP(pseudoCandles, periods));
        }

        /// <summary>
        /// Calculates the On-Balance Volume (OBV), which accumulates volume based on price direction.
        /// Volume is added when price closes higher, subtracted when lower.
        /// Rising OBV indicates buying pressure, falling OBV indicates selling pressure.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price and volume data.</param>
        /// <returns>The OBV value (can be positive or negative based on net volume flow).</returns>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// <see cref="CalculateOBVAsync(List{PseudoCandlestick})"/>
        /// </remarks>
        public decimal CalculateOBV(List<PseudoCandlestick> pseudoCandles)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for OBV calculation.");
                        return 0;
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
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
        /// Calculates the On-Balance Volume (OBV) asynchronously, which accumulates volume based on price direction.
        /// Volume is added when price closes higher, subtracted when lower.
        /// Rising OBV indicates buying pressure, falling OBV indicates selling pressure.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing price and volume data.</param>
        /// <returns>A task that represents the asynchronous operation, containing the OBV value (can be positive or negative based on net volume flow).</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateOBV(List{PseudoCandlestick})"/>
        /// </remarks>
        public async Task<decimal> CalculateOBVAsync(List<PseudoCandlestick> pseudoCandles)
        {
            return await Task.Run(() => CalculateOBV(pseudoCandles));
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
            var validCandlesticks = candlesticks.ToList();

            // Use all historical candlesticks for comprehensive analysis
            // validCandlesticks = validCandlesticks.OrderByDescending(c => c.Date).Take(1000).ToList();

            log.AppendLine($"Filtered out {candlesticks.Count - validCandlesticks.Count} candlesticks between 07:00:00 and 11:59:00 UTC. Valid candlesticks: {validCandlesticks.Count}");

            if (validCandlesticks.Count < 3)
            {
                _logger.LogDebug("Insufficient valid candlesticks after filtering: {Count}, Required: 3.", validCandlesticks.Count);
                return new List<SupportResistanceLevel>();
            }

            // Calculate minimum touches based on percentage of valid candlesticks
            int minTouches = Math.Max(1, (int)Math.Round(validCandlesticks.Count * minCandlestickPercentage));
            log.AppendLine($"Minimum touches required: {minTouches} ({minCandlestickPercentage:P0} of {validCandlesticks.Count} valid candlesticks)");

            // Collect frequency and volume for each normalized price (1 to 99 cents) with time-based weighting
            var priceFrequency = new Dictionary<int, double>();
            var priceVolume = new Dictionary<int, long>();

            // Sort candlesticks by date to apply time-based weighting
            var sortedCandlesticks = validCandlesticks.OrderBy(c => c.Date).ToList();
            var earliestDate = sortedCandlesticks.First().Date;
            var latestDate = sortedCandlesticks.Last().Date;
            var totalTimeSpan = (latestDate - earliestDate).TotalHours;

            foreach (var candle in sortedCandlesticks)
            {
                int normalizedPrice = (int)Math.Round((candle.AskClose + candle.BidClose) / 2.0);
                if (normalizedPrice < 1 || normalizedPrice > 99)
                    continue;

                // Calculate time-based weight (exponential decay favoring recent data)
                var hoursSinceStart = (candle.Date - earliestDate).TotalHours;
                var timeWeight = Math.Exp(hoursSinceStart / Math.Max(totalTimeSpan, 1.0) * _config.ResistanceLevels_ExponentialMultiplier); // Exponential weighting

                if (priceFrequency.ContainsKey(normalizedPrice))
                {
                    priceFrequency[normalizedPrice] += timeWeight;
                    priceVolume[normalizedPrice] += candle.Volume;
                }
                else
                {
                    priceFrequency[normalizedPrice] = timeWeight;
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

                // Calculate strength as smoothed frequency
                double strength = smoothedFrequency[peakIndex];

                // Calculate total volume within ±1 cent of the peak price
                long totalVolume = 0;
                for (int p = Math.Max(minPrice, price - 1); p <= Math.Min(maxPrice, price + 1); p++)
                {
                    int idx = p - minPrice;
                    totalVolume += (long)volumeArray[idx];
                }

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

            // Filter out levels too close to 0 or 100
            selectedLevels = selectedLevels.Where(l => l.Price > 5 && l.Price < 95).ToList();

            // Sort by strength
            selectedLevels = selectedLevels.OrderByDescending(l => l.Strength).ToList();

            log.AppendLine($"Selected {selectedLevels.Count} unified levels.");
            _logger.LogDebug("{Log}", log.ToString());
            return selectedLevels;
        }

        /// <summary>
        /// Calculates statistically significant historical support and resistance levels asynchronously for a given market ticker.
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
        /// <returns>A task that represents the asynchronous operation, containing a list of <see cref="SupportResistanceLevel"/> objects, sorted by strength in descending order.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateHistoricalSupportResistance(string, List{CandlestickData}, double, int, double, int)"/>
        /// </remarks>
        public async Task<List<SupportResistanceLevel>> CalculateHistoricalSupportResistanceAsync(
            string marketTicker,
            List<CandlestickData> candlesticks,
            double minCandlestickPercentage,
            int maxLevels,
            double sigma,
            int minDistance)
        {
            return await Task.Run(() => CalculateHistoricalSupportResistance(marketTicker, candlesticks, minCandlestickPercentage, maxLevels, sigma, minDistance));
        }

        /// <summary>
        /// Calculates the Parabolic Stop and Reverse (PSAR) value for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator is valuable for event-driven contracts where price trends can emerge rapidly due to evolving news or data releases influencing outcome probabilities.
        /// It identifies trend directions and provides dynamic trailing stop levels, aiding risk management in volatile, bounded-price environments (1 to 99 cents per contract).
        /// However, in ranging or sideways markets, common when events are distant or uncertain, PSAR may generate frequent false reversal signals (whipsaws), potentially leading to unnecessary trades.
        /// It performs best when combined with trend-confirming indicators like the Average Directional Index (ADX) to filter signals.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing mid-high, mid-low, and mid-close prices.</param>
        /// <returns>The current PSAR value, or null if insufficient data or invalid computation.</returns>
        /// <remarks>
        /// Input validation includes null check for the pseudoCandles list and parameter validation with exceptions.
        /// Parameters are sourced from configuration where applicable (e.g., PSAR acceleration factors).
        /// Different PSAR values indicate the following in Kalshi's context:
        /// - A PSAR value below the current price (e.g., PSAR at 45 when the mid-close is 60) suggests an uptrend, signaling potential buying opportunities or holding long ("Yes") positions, with the PSAR serving as a rising trailing stop.
        /// - A PSAR value above the current price (e.g., PSAR at 75 when the mid-close is 60) indicates a downtrend, implying selling pressure or short ("No") positions, with the PSAR acting as a falling trailing stop.
        /// - Crossovers, where the price moves beyond the PSAR, denote potential trend reversals: a price rising above PSAR flips to bullish, while falling below flips to bearish.
        /// - Acceleration factor (AF) increases (up to the maximum, typically 0.20) amplify the PSAR's sensitivity in strong trends, tightening stops to lock in gains.
        /// <see cref="CalculatePSARAsync(List{PseudoCandlestick})"/>
        /// </remarks>
        public double? CalculatePSAR(List<PseudoCandlestick> pseudoCandles)
        {
            var log = new StringBuilder();
            log.AppendLine($"Calculating PSAR with InitialAF={_config.PSAR_InitialAF}, MaxAF={_config.PSAR_MaxAF}, AFStep={_config.PSAR_AFStep}.");

            if (_config.PSAR_InitialAF <= 0 || _config.PSAR_MaxAF <= 0 || _config.PSAR_AFStep <= 0 || _config.PSAR_InitialAF > _config.PSAR_MaxAF)
            {
                log.AppendLine("Invalid acceleration parameters.");
                _logger.LogError("{Log}", log.ToString());
                throw new ArgumentException("Acceleration factors must be positive and InitialAF <= MaxAF.");
            }

            if (pseudoCandles == null || pseudoCandles.Count < 2)
            {
                log.AppendLine($"Insufficient data. Candles: {pseudoCandles?.Count ?? 0}, Required: 2.");
                _logger.LogDebug("{Log}", log.ToString());
                return null;
            }

            var highs = pseudoCandles.Select(pc => pc.MidHigh).ToList();
            var lows = pseudoCandles.Select(pc => pc.MidLow).ToList();
            var closes = pseudoCandles.Select(pc => pc.MidClose).ToList();

            double psar = lows[0]; // Initialize with first low
            int trend = closes[1] > closes[0] ? 1 : -1; // 1 for uptrend, -1 for downtrend
            double ep = trend == 1 ? highs[0] : lows[0]; // Extreme point
            double af = _config.PSAR_InitialAF;

            for (int i = 1; i < pseudoCandles.Count; i++)
            {
                double currentPsar = psar + af * (ep - psar);

                if (trend == 1) // Uptrend
                {
                    if (lows[i] < currentPsar) // Reversal to downtrend
                    {
                        trend = -1;
                        psar = i > 0 ? Math.Max(highs[i - 1], highs[i]) : highs[i];
                        ep = lows[i];
                        af = _config.PSAR_InitialAF;
                    }
                    else
                    {
                        psar = currentPsar;
                        if (highs[i] > ep)
                        {
                            ep = highs[i];
                            af = Math.Min(af + _config.PSAR_AFStep, _config.PSAR_MaxAF);
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
                        af = _config.PSAR_InitialAF;
                    }
                    else
                    {
                        psar = currentPsar;
                        if (lows[i] < ep)
                        {
                            ep = lows[i];
                            af = Math.Min(af + _config.PSAR_AFStep, _config.PSAR_MaxAF);
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
        /// Calculates the Parabolic Stop and Reverse (PSAR) value asynchronously for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator is valuable for event-driven contracts where price trends can emerge rapidly due to evolving news or data releases influencing outcome probabilities.
        /// It identifies trend directions and provides dynamic trailing stop levels, aiding risk management in volatile, bounded-price environments (1 to 99 cents per contract).
        /// However, in ranging or sideways markets common when events are distant or uncertain, PSAR may generate frequent false reversal signals (whipsaws), potentially leading to unnecessary trades.
        /// It performs best when combined with trend-confirming indicators like the Average Directional Index (ADX) to filter signals.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing mid-high, mid-low, and mid-close prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing the current PSAR value, or null if insufficient data or invalid computation.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculatePSAR(List{PseudoCandlestick})"/>
        /// </remarks>
        public async Task<double?> CalculatePSARAsync(List<PseudoCandlestick> pseudoCandles)
        {
            return await Task.Run(() => CalculatePSAR(pseudoCandles));
        }

        /// <summary>
        /// Calculates the Average Directional Index (ADX) value along with +DI and -DI for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator serves as a robust complementary metric, particularly when integrated with tools like the Parabolic Stop and Reverse (PSAR) to validate trend strength.
        /// It provides an objective measure of trend intensity, irrespective of direction, which aids in distinguishing genuine trends from periods of consolidation or volatility without momentum.
        /// This is especially pertinent in Kalshi's event-driven environment, where contract prices may exhibit pronounced trends due to evolving probabilities influenced by economic announcements or other developments.
        /// By confirming the presence of a strong trend (typically ADX values exceeding 25), it enhances the efficacy of PSAR signals, reducing the likelihood of whipsaws in non-trending conditions.
        /// The +DI and -DI components provide directional information: +DI > -DI suggests uptrend, -DI > +DI suggests downtrend.
        /// Its utility diminishes in highly volatile, range-bound markets common to certain Kalshi contracts awaiting resolution.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing mid-high, mid-low, and mid-close prices.</param>
        /// <returns>A tuple containing ADX, +DI, and -DI values, or null values if insufficient data or invalid computation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when pseudoCandles is null.</exception>
        /// <remarks>
        /// Input validation includes null check for the pseudo-candlesticks list, empty list handling with warnings, and data integrity checks with warnings for invalid candlestick data (e.g., negative prices, invalid timestamps).
        /// Parameters are sourced from configuration where applicable (e.g., ADX period).
        /// Interpretations of ADX values are as follows in Kalshi's context:
        /// - Below 20: Indicates a weak or absent trend, suggesting consolidation or choppy conditions; traders should exercise caution with trend-following strategies like PSAR, as signals may prove unreliable.
        /// - Between 20 and 25: Signals the potential emergence of a trend; this transitional range warrants monitoring for confirmation, such as a PSAR crossover aligned with rising ADX.
        /// - Between 25 and 40: Reflects a strong trend, validating PSAR positions and encouraging trend continuation trades, with higher values denoting increased momentum.
        /// - Above 40: Denotes an exceptionally strong trend, often nearing potential exhaustion; while supportive of PSAR trailing stops, it may foreshadow reversals, prompting consideration of profit-taking.
        ///
        /// +DI and -DI interpretations:
        /// - +DI > -DI: Suggests upward trend strength
        /// - -DI > +DI: Suggests downward trend strength
        /// - +DI crossing above -DI: Potential bullish signal
        /// - -DI crossing above +DI: Potential bearish signal
        /// <see cref="CalculateADXAsync(List{PseudoCandlestick})"/>
        /// </remarks>
        public (double? ADX, double? PlusDI, double? MinusDI) CalculateADX(List<PseudoCandlestick> pseudoCandles)
                {
                    if (pseudoCandles == null)
                    {
                        throw new ArgumentNullException(nameof(pseudoCandles), "The list of pseudo-candlesticks cannot be null.");
                    }
                    if (pseudoCandles.Count == 0)
                    {
                        _logger.LogWarning("Empty list of pseudo-candlesticks provided for ADX calculation.");
                        return (null, null, null);
                    }
                    for (int i = 0; i < pseudoCandles.Count; i++)
                    {
                        var pc = pseudoCandles[i];
                        double openPrice = i == 0 ? pc.MidClose : pseudoCandles[i-1].MidClose;
                        if (pc.MidClose <= 0 || pc.MidHigh <= 0 || pc.MidLow <= 0 || openPrice <= 0 || pc.MidHigh < pc.MidLow || pc.Timestamp == DateTime.MinValue)
                        {
                            _logger.LogWarning("Invalid PseudoCandlestick at index {Index}: Close={Close}, High={High}, Low={Low}, Open={Open}, Timestamp={Timestamp}", i, pc.MidClose, pc.MidHigh, pc.MidLow, openPrice, pc.Timestamp);
                        }
                    }
                    var log = new StringBuilder();
                    int period = _config.ADX_Periods;
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
                return (null,null,null);
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
            double? latestPlusDI = null;
            double? latestMinusDI = null;

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

                // Store the latest +DI and -DI values
                latestPlusDI = plusDI;
                latestMinusDI = minusDI;

                log.AppendLine($"Index {i}: +DM={plusDM:F4}, -DM={minusDM:F4}, TR={tr:F4}, +DI={plusDI:F2}, -DI={minusDI:F2}, DX={dx:F2}");
            }

            if (dxValues.Count < period)
            {
                log.AppendLine($"Insufficient DX values for ADX: {dxValues.Count}, Required: {period}.");
                _logger.LogDebug("{Log}", log.ToString());
                return (null, null, null);
            }

            // Calculate ADX as SMA of last 'period' DX values
            double adx = dxValues.TakeLast(period).Average();
            if (double.IsNaN(adx) || double.IsInfinity(adx))
            {
                log.AppendLine($"Invalid ADX: {adx}.");
                _logger.LogDebug("{Log}", log.ToString());
                return (null, null, null);
            }

            log.AppendLine($"ADX: {adx:F2}, +DI: {latestPlusDI:F2}, -DI: {latestMinusDI:F2}");
            _logger.LogDebug("{Log}", log.ToString());
            return (adx, latestPlusDI, latestMinusDI);
        }

        /// <summary>
        /// Calculates the Average Directional Index (ADX) value asynchronously along with +DI and -DI for a series of pseudo-candlesticks in the Kalshi marketplace.
        /// This indicator serves as a robust complementary metric, particularly when integrated with tools like the Parabolic Stop and Reverse (PSAR) to validate trend strength.
        /// It provides an objective measure of trend intensity, irrespective of direction, which aids in distinguishing genuine trends from periods of consolidation or volatility without momentum.
        /// This is especially pertinent in Kalshi's event-driven environment, where contract prices may exhibit pronounced trends due to evolving probabilities influenced by economic announcements or other developments.
        /// By confirming the presence of a strong trend (typically ADX values exceeding 25), it enhances the efficacy of PSAR signals, reducing the likelihood of whipsaws in non-trending conditions.
        /// The +DI and -DI components provide directional information: +DI > -DI suggests uptrend, -DI > +DI suggests downtrend.
        /// Its utility diminishes in highly volatile, range-bound markets common to certain Kalshi contracts awaiting resolution.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo-candlesticks containing mid-high, mid-low, and mid-close prices.</param>
        /// <returns>A task that represents the asynchronous operation, containing a tuple of ADX, +DI, and -DI values, or null values if insufficient data or invalid computation.</returns>
        /// <remarks>
        /// Asynchronous execution using Task.Run for performance in high-frequency scenarios.
        /// <see cref="CalculateADX(List{PseudoCandlestick})"/>
        /// </remarks>
        public async Task<(double? ADX, double? PlusDI, double? MinusDI)> CalculateADXAsync(List<PseudoCandlestick> pseudoCandles)
        {
            return await Task.Run(() => CalculateADX(pseudoCandles));
        }
    }
}
