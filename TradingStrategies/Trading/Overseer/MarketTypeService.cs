using BacklashDTOs;
using TradingStrategies.Trading.Helpers;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Service responsible for determining and caching market types for trading snapshots.
    /// This class acts as a facade over the MarketTypeHelper, providing caching functionality
    /// to avoid redundant market type calculations for the same market snapshot.
    /// </summary>
    /// <remarks>
    /// The MarketTypeService is used in the trading simulation pipeline to classify market conditions
    /// based on various indicators (price movement, liquidity, activity, etc.). It leverages a helper
    /// class to perform the actual classification logic and maintains an in-memory cache to optimize
    /// performance during simulation runs where the same snapshot may be processed multiple times.
    ///
    /// Key responsibilities:
    /// - Assign market types to MarketSnapshot instances
    /// - Cache market type results to reduce computational overhead
    /// - Convert string representations back to MarketType enums
    ///
    /// This service is instantiated once per simulation engine and reused throughout the simulation process.
    /// </remarks>
    public class MarketTypeService
    {
        /// <summary>
        /// Helper instance that performs the actual market type classification logic.
        /// </summary>
        /// <remarks>
        /// The MarketTypeHelper contains the rule-based system for determining market types
        /// based on various market conditions extracted from snapshots.
        /// </remarks>
        private readonly MarketTypeHelper _marketTypeHelper;

        /// <summary>
        /// Cache storing market type results keyed by market ticker and timestamp.
        /// </summary>
        /// <remarks>
        /// This dictionary prevents redundant calculations when the same market snapshot
        /// is processed multiple times during simulation. The key combines ticker and timestamp
        /// to uniquely identify a market state.
        /// </remarks>
        private readonly Dictionary<(string Ticker, DateTime Timestamp), MarketType> _marketTypeCache;

        /// <summary>
        /// Initializes a new instance of the MarketTypeService class.
        /// </summary>
        /// <remarks>
        /// Creates the MarketTypeHelper instance and initializes the cache dictionary.
        /// This constructor sets up the service for immediate use in market type classification.
        /// </remarks>
        public MarketTypeService()
        {
            _marketTypeHelper = new MarketTypeHelper();
            _marketTypeCache = new Dictionary<(string, DateTime), MarketType>();
        }

        /// <summary>
        /// Assigns the appropriate market type to the provided market snapshot.
        /// </summary>
        /// <param name="snapshot">The market snapshot to classify and update with market type information.</param>
        /// <remarks>
        /// This method first checks the cache for an existing classification result. If not found,
        /// it delegates to the MarketTypeHelper to determine the market type based on the snapshot's
        /// trading conditions. The result is then cached and assigned to the snapshot's MarketType property.
        ///
        /// If an exception occurs during classification (e.g., due to missing or invalid data),
        /// the market type is set to "Unknown" as a fallback to ensure simulation continuity.
        ///
        /// The method modifies the input snapshot in-place, setting its MarketType property to the
        /// string representation of the determined MarketType enum value.
        /// </remarks>
        public void AssignMarketTypeToSnapshot(MarketSnapshot snapshot)
        {
            try
            {
                if (string.IsNullOrEmpty(snapshot.MarketTicker))
                {
                    snapshot.MarketType = MarketType.Undefined.ToString();
                    return;
                }

                var key = (snapshot.MarketTicker, snapshot.Timestamp);
                if (!_marketTypeCache.TryGetValue(key, out var cachedType))
                {
                    cachedType = _marketTypeHelper.GetMarketType(snapshot);
                    _marketTypeCache[key] = cachedType;
                }
                snapshot.MarketType = cachedType.ToString();
            }
            catch
            {
                snapshot.MarketType = MarketType.Undefined.ToString();
            }
        }

        /// <summary>
        /// Converts a string representation of a market type to its corresponding MarketType enum value.
        /// </summary>
        /// <param name="marketType">The string representation of the market type to convert.</param>
        /// <returns>The MarketType enum value corresponding to the input string, or MarketType.Undefined if parsing fails.</returns>
        /// <remarks>
        /// This method performs case-insensitive parsing of the market type string using Enum.TryParse.
        /// It serves as a utility for converting market type strings (e.g., from serialized data or user input)
        /// back to strongly-typed enum values for use in conditional logic and strategy selection.
        ///
        /// If the string cannot be parsed to a valid MarketType value, MarketType.Undefined is returned
        /// as a safe default to prevent exceptions in calling code.
        ///
        /// Common use cases include:
        /// - Deserializing market type data from persistent storage
        /// - Processing market type strings from external APIs
        /// - Converting user-provided market type filters
        /// </remarks>
        public MarketType ConvertStringToMarketType(string marketType)
        {
            if (!Enum.TryParse<MarketType>(marketType, true, out var currentMarketConditions))
            {
                currentMarketConditions = MarketType.Undefined;
            }
            return currentMarketConditions;
        }
    }
}