using BacklashDTOs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TradingStrategies.Configuration;

namespace TradingStrategies.Extensions
{
    /// <summary>
    /// Provides extension methods for converting PseudoCandlestick collections to standard candlestick formats.
    /// This class serves as a bridge between simplified mid-price candlestick data and traditional OHLC candlestick data,
    /// enabling pattern detection and technical analysis algorithms to work with market data.
    /// </summary>
    public static class PseudoCandlestickExtensions
    {
        /// <summary>
        /// Converts a sequence of PseudoCandlesticks to an array of CandleMids with OHLC data.
        /// Each resulting candlestick represents the price movement between consecutive PseudoCandlesticks,
        /// using the previous candlestick's mid-close as the open price and the current candlestick's
        /// mid-close as the close price, with high/low from the current candlestick.
        /// </summary>
        /// <param name="candles">The sequence of PseudoCandlesticks to convert. Must contain at least 2 elements.</param>
        /// <param name="marketTicker">The market ticker identifier to assign to each resulting CandleMids.</param>
        /// <param name="config">Optional trading configuration for volume precision handling.</param>
        /// <param name="logger">Optional logger for performance metrics collection.</param>
        /// <returns>An array of CandleMids representing the price movements between consecutive input candlesticks.</returns>
        /// <exception cref="ArgumentNullException">Thrown when candles is null.</exception>
        /// <exception cref="ArgumentException">Thrown when candles contains fewer than 2 elements or marketTicker is null/empty.</exception>
        /// <remarks>
        /// This method creates n-1 candlesticks from n input PseudoCandlesticks, where each output candlestick
        /// represents the transition from one time period to the next. The volume is cast from decimal to double
        /// to match the CandleMids data type expectations, with optional precision rounding based on config.
        /// Performance metrics are logged if the conversion takes longer than 100ms for large sequences.
        /// </remarks>
        public static CandleMids[] ToCandleMids(
            this IList<PseudoCandlestick> candles,
            string marketTicker,
            ILogger logger = null)
        {
            if (candles == null || candles.Count < 2)
                return Array.Empty<CandleMids>();

            if (string.IsNullOrEmpty(marketTicker))
                throw new ArgumentException("Market ticker cannot be null or empty.", nameof(marketTicker));

            var stopwatch = Stopwatch.StartNew();

            var result = new List<CandleMids>(candles.Count - 1);

            for (int i = 1; i < candles.Count; i++)
            {
                var prev = candles[i - 1];
                var curr = candles[i];

                result.Add(new CandleMids
                {
                    MarketTicker = marketTicker,
                    Timestamp = curr.Timestamp,
                    Open = prev.MidClose,
                    Close = curr.MidClose,
                    High = curr.MidHigh,
                    Low = curr.MidLow,
                    Volume = Math.Round((double)curr.Volume, 2) 
                });
            }

            stopwatch.Stop();
            if (logger != null && stopwatch.ElapsedMilliseconds > 100)
            {
                logger.LogInformation($"PseudoCandlestick to CandleMids conversion took {stopwatch.ElapsedMilliseconds} ms for {candles.Count} candlesticks.");
            }

            return result.ToArray();
        }
    }
}
