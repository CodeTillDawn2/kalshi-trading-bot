namespace TradingSimulator.Strategies
{
    /// <summary>
    /// Defines the possible trading decisions that can be made by a trading strategy.
    /// This enum is used throughout the trading simulator to represent the core actions
    /// a strategy can recommend: purchasing an asset, maintaining the current position,
    /// or selling an existing position. It serves as the fundamental decision type
    /// for strategy evaluation and trade execution logic.
    /// </summary>
    public enum TradingDecisionEnum
    {
        /// <summary>
        /// Indicates a recommendation to purchase or enter a long position in the asset.
        /// This decision is typically generated when strategy signals suggest upward price movement
        /// or favorable market conditions for buying.
        /// </summary>
        Buy,

        /// <summary>
        /// Indicates a recommendation to maintain the current position without making changes.
        /// This decision is used when strategy signals do not strongly suggest buying or selling,
        /// or when the market conditions are neutral or uncertain.
        /// </summary>
        Hold,

        /// <summary>
        /// Indicates a recommendation to sell or exit a long position in the asset.
        /// This decision is typically generated when strategy signals suggest downward price movement
        /// or unfavorable market conditions for holding the current position.
        /// </summary>
        Sell
    }
}
