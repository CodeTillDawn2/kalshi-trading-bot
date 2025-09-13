using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    /// <summary>
    /// Represents a trading scenario that organizes strategies based on different market conditions.
    /// This class serves as a container for mapping market types to their corresponding trading strategies,
    /// allowing the system to select appropriate strategies based on current market conditions during simulation or live trading.
    /// </summary>
    public class Scenario
    {
        /// <summary>
        /// Gets or sets the dictionary mapping market types to lists of strategies.
        /// Each market type (e.g., Trending, Volatile, LowLiquidity) can have multiple strategies
        /// that are suitable for those specific market conditions.
        /// </summary>
        public Dictionary<MarketType, List<Strategy>> StrategiesByMarketConditions { get; set; }

        /// <summary>
        /// Initializes a new instance of the Scenario class with the specified strategy mappings.
        /// </summary>
        /// <param name="strategiesByMarketConditions">A dictionary mapping market types to their corresponding strategies.</param>
        public Scenario(Dictionary<MarketType, List<Strategy>> strategiesByMarketConditions)
        {
            StrategiesByMarketConditions = strategiesByMarketConditions;
        }
    }
}
