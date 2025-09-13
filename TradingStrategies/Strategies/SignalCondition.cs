using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    /// <summary>
    /// Defines a condition based on a trading signal that triggers a specific trading action.
    /// This class combines a signal (technical indicator or market condition), a comparison operator,
    /// a threshold value, and the resulting action type to create rule-based trading logic.
    /// Signal conditions are used by strategies to evaluate market states and determine appropriate responses.
    /// </summary>
    public class SignalCondition
    {
        /// <summary>
        /// Gets the trading signal that this condition evaluates.
        /// </summary>
        public Signal Signal { get; }

        /// <summary>
        /// Gets the comparison operator used to evaluate the signal against the threshold.
        /// </summary>
        public ComparisonOperator Operator { get; }

        /// <summary>
        /// Gets the threshold value used in the comparison with the signal.
        /// </summary>
        public int Threshold { get; }

        /// <summary>
        /// Gets the trading action that should be taken when this condition is met.
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        /// Initializes a new instance of the SignalCondition class.
        /// </summary>
        /// <param name="signal">The trading signal to evaluate.</param>
        /// <param name="op">The comparison operator for the evaluation.</param>
        /// <param name="threshold">The threshold value for the comparison.</param>
        /// <param name="action">The trading action to take when the condition is met.</param>
        public SignalCondition(Signal signal, ComparisonOperator op, int threshold, ActionType action)
        {
            Signal = signal;
            Operator = op;
            Threshold = threshold;
            ActionType = action;
        }
    }
}
