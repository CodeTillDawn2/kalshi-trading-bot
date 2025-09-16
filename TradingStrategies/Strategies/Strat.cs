using BacklashDTOs;
using TradingStrategies.Trading.Overseer;

namespace TradingStrategies.Strategies
{
    /// <summary>
    /// Abstract base class for individual trading strategy implementations.
    /// Concrete strategy classes inherit from this class to provide specific trading logic
    /// that evaluates market conditions and returns trading decisions. Each strategy has a weight
    /// that influences its importance when combined with other strategies in a composite approach.
    /// </summary>
    public abstract class Strat
    {
        /// <summary>
        /// Gets the weight of this strategy, used to determine its influence in composite decision-making.
        /// Higher weights give more importance to this strategy's decisions when multiple strategies are combined.
        /// </summary>
        public abstract double Weight { get; }

        /// <summary>
        /// Evaluates the current market snapshot and returns a trading action decision.
        /// </summary>
        /// <param name="snapshot">The current market snapshot containing price, volume, and technical indicator data.</param>
        /// <param name="previousSnapshot">The previous market snapshot for comparison, or null if this is the first snapshot.</param>
        /// <param name="simulationPosition">The current position in the simulation (0 for no position, positive for long, negative for short).</param>
        /// <returns>An ActionDecision containing the recommended trading action and any associated metadata.</returns>
        public abstract ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0);

        /// <summary>
        /// Serializes the strategy's configuration and parameters to a JSON string.
        /// This is used for persistence, logging, and strategy reconstruction.
        /// </summary>
        /// <returns>A JSON string representation of the strategy's state and configuration.</returns>
        public abstract string ToJson();
    }
}
