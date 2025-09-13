using BacklashDTOs;
using BacklashPatterns;
using TradingStrategies.Extensions;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Service responsible for detecting candlestick patterns from market snapshots in the Kalshi trading bot system.
    /// This service acts as a bridge between market data and pattern recognition algorithms, enabling trading
    /// strategies to incorporate technical analysis based on historical price movements.
    ///
    /// The service processes market snapshots containing recent candlestick data, converts them to a format
    /// suitable for pattern analysis, and applies comprehensive pattern detection algorithms to identify
    /// various technical analysis formations including single-candle, multi-candle, and complex patterns.
    ///
    /// Key responsibilities:
    /// - Convert market snapshot candlestick data to pattern analysis format
    /// - Execute pattern detection algorithms across multiple timeframes and pattern types
    /// - Filter and return the most relevant patterns for trading decision making
    /// - Handle edge cases and provide graceful fallbacks for data processing errors
    ///
    /// This service is used by the SimulationEngine during backtesting and strategy evaluation to enrich
    /// market analysis with technical pattern recognition capabilities.
    /// </summary>
    /// <remarks>
    /// The pattern detection process involves several steps:
    /// 1. Validation of input data (candlestick availability)
    /// 2. Conversion of PseudoCandlesticks to CandleMids format for analysis
    /// 3. Application of pattern recognition algorithms with trend analysis
    /// 4. Pattern filtering to remove redundancies and prioritize significant formations
    /// 5. Return of filtered patterns for strategy consumption
    ///
    /// Error handling ensures that pattern detection failures don't interrupt the overall trading simulation,
    /// with appropriate logging for debugging and monitoring purposes.
    /// </remarks>
    public class PatternDetectionService
    {
        /// <summary>
        /// Detects candlestick patterns from the provided market snapshot.
        /// Analyzes recent candlestick data to identify various technical analysis patterns
        /// that can inform trading strategy decisions.
        /// </summary>
        /// <param name="snapshot">The market snapshot containing recent candlestick data and market information.</param>
        /// <returns>A list of detected pattern definitions representing significant technical formations.</returns>
        /// <remarks>
        /// The detection process involves:
        /// - Converting PseudoCandlesticks to CandleMids format for pattern analysis
        /// - Applying comprehensive pattern recognition algorithms across multiple pattern types
        /// - Using a 10-period lookback window for trend context and pattern validation
        /// - Filtering patterns to return only the most recent and relevant formations
        ///
        /// If no candlestick data is available or pattern detection fails, an empty list is returned
        /// to ensure graceful degradation in the trading simulation pipeline.
        ///
        /// The method handles various edge cases:
        /// - Null or empty candlestick collections
        /// - Pattern detection algorithm failures
        /// - Data conversion errors
        ///
        /// All exceptions are caught and logged to prevent interruption of the simulation process,
        /// with appropriate fallback behavior ensuring system stability.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null (handled internally).</exception>
        /// <exception cref="Exception">Any pattern detection errors are caught and logged internally.</exception>
        public List<BacklashPatterns.PatternDefinitions.PatternDefinition> DetectPatterns(MarketSnapshot snapshot)
        {
            // Validate input data availability
            if (snapshot.RecentCandlesticks == null || snapshot.RecentCandlesticks.Count == 0)
            {
                return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
            }

            try
            {
                // Convert PseudoCandlesticks to CandleMids format for pattern analysis
                var mids = snapshot.RecentCandlesticks.ToCandleMids(snapshot.MarketTicker ?? string.Empty);

                // Execute pattern detection with 10-period trend lookback for context
                var patterns = PatternSearch.DetectPatterns(mids, 10);

                // Return the most recent set of detected patterns if any were found
                if (patterns.Keys.Count > 0)
                {
                    return patterns[patterns.Keys.Last()];
                }
            }
            catch (Exception ex)
            {
                // Log pattern detection errors while maintaining system stability
                Console.WriteLine($"Error detecting patterns: {ex.Message}");
            }

            // Return empty list as fallback for any processing failures
            return new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
        }
    }
}