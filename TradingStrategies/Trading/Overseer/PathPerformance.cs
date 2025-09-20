using System.Collections.Generic;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Represents the performance metrics for a complete trading path or simulation run.
    /// This class aggregates key performance indicators including profit/loss, equity changes,
    /// trade counts, and market state transitions for comprehensive analysis of trading outcomes.
    /// </summary>
    public class PathPerformance
    {
        /// <summary>
        /// The unique identifier of the market this performance relates to.
        /// </summary>
        public string? MarketId { get; set; }

        /// <summary>
        /// The sequence of market types or path taken during the simulation.
        /// </summary>
        public string? PathTaken { get; set; }

        /// <summary>
        /// Dictionary mapping market types to the number of snapshots taken for each type.
        /// </summary>
        public Dictionary<string, int>? SnapshotsPerType { get; set; }

        /// <summary>
        /// The profit and loss for this trading path.
        /// </summary>
        public double PnL { get; set; }

        /// <summary>
        /// The final equity value after completing this trading path.
        /// </summary>
        public double Equity { get; set; }

        /// <summary>
        /// The total number of trades executed in this path.
        /// </summary>
        public int Trades { get; set; }

        /// <summary>
        /// The Yes bid price at the start of this path.
        /// </summary>
        public int StartYesBid { get; set; }

        /// <summary>
        /// The No bid price at the start of this path.
        /// </summary>
        public int StartNoBid { get; set; }

        /// <summary>
        /// The Yes bid price at the end of this path.
        /// </summary>
        public int EndYesBid { get; set; }

        /// <summary>
        /// The No bid price at the end of this path.
        /// </summary>
        public int EndNoBid { get; set; }

        /// <summary>
        /// The type or outcome of the market at the end of this path.
        /// </summary>
        public string? EndType { get; set; }

        /// <summary>
        /// The simulated position size at the end of this path.
        /// </summary>
        public int SimulatedPosition { get; set; }

        /// <summary>
        /// The average cost of the position throughout this path.
        /// </summary>
        public double AverageCost { get; set; }
    }
}
