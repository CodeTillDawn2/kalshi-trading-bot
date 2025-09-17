using System;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Represents a grouped collection of related event logs that share the same market type and action.
    /// This class aggregates multiple SimulationEventLog entries into summary statistics for analysis and reporting,
    /// providing averaged metrics and position changes over a time period.
    /// </summary>
    public class EventGroup
    {
        /// <summary>
        /// The market type shared by all events in this group.
        /// </summary>
        public string MarketType { get; set; } = null!;

        /// <summary>
        /// The action shared by all events in this group.
        /// </summary>
        public string Action { get; set; } = null!;

        /// <summary>
        /// The timestamp of the first event in this group.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The timestamp of the last event in this group.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// The number of event snapshots included in this group.
        /// </summary>
        public int SnapshotCount { get; set; }

        /// <summary>
        /// The position at the start of this event group.
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// The position at the end of this event group.
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// The cash balance at the start of this event group.
        /// </summary>
        public double StartCash { get; set; }

        /// <summary>
        /// The cash balance at the end of this event group.
        /// </summary>
        public double EndCash { get; set; }

        /// <summary>
        /// The average liquidity across all events in this group.
        /// </summary>
        public double AvgLiquidity { get; set; }

        /// <summary>
        /// The average RSI across all events in this group.
        /// </summary>
        public double AvgRSI { get; set; }

        /// <summary>
        /// The strategies applied during this event group.
        /// </summary>
        public string Strategies { get; set; } = null!;

        /// <summary>
        /// The average Yes spread across all events in this group.
        /// </summary>
        public double AvgYesSpread { get; set; }

        /// <summary>
        /// The average No spread across all events in this group.
        /// </summary>
        public double AvgNoSpread { get; set; }

        /// <summary>
        /// The average trade rate for the Yes side across all events in this group.
        /// </summary>
        public double AvgTradeRateYes { get; set; }

        /// <summary>
        /// The average trade rate for the No side across all events in this group.
        /// </summary>
        public double AvgTradeRateNo { get; set; }

        /// <summary>
        /// The average depth at No bid across all events in this group.
        /// </summary>
        public double AvgDepthNoBid { get; set; }

        /// <summary>
        /// The average bid imbalance across all events in this group.
        /// </summary>
        public double AvgBidImbalance { get; set; }

        /// <summary>
        /// The average Yes bid price in dollars across all events in this group.
        /// </summary>
        public double AvgYesBidPrice { get; set; }

        /// <summary>
        /// The average No bid price in dollars across all events in this group.
        /// </summary>
        public double AvgNoBidPrice { get; set; }

        /// <summary>
        /// The average quantity of resting Yes bids across all events in this group.
        /// </summary>
        public double AvgRestingYesQty { get; set; }

        /// <summary>
        /// The average quantity of resting No bids across all events in this group.
        /// </summary>
        public double AvgRestingNoQty { get; set; }
    }
}