using System;
using System.Collections.Generic;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Represents a single event log entry from a trading simulation, capturing the complete state
    /// of the trading environment at a specific point in time. This data structure is used to record
    /// and analyze the progression of trading activities, market conditions, and strategy decisions
    /// throughout a simulation run.
    /// </summary>
    public class SimulationEventLog
    {
        /// <summary>
        /// The timestamp when this event occurred during the simulation.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The type of market condition or category this event represents (e.g., "Trending", "Ranging").
        /// </summary>
        public string MarketType { get; set; } = null!;

        /// <summary>
        /// The trading action taken at this event (e.g., "Buy", "Sell", "Hold").
        /// </summary>
        public string Action { get; set; } = null!;

        /// <summary>
        /// The current position size after this event (positive for long, negative for short).
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The cash balance available at this event.
        /// </summary>
        public double Cash { get; set; }

        /// <summary>
        /// The market liquidity measure at this event.
        /// </summary>
        public double Liquidity { get; set; }

        /// <summary>
        /// The bid-ask spread for the Yes side of the market.
        /// </summary>
        public int YesSpread { get; set; }

        /// <summary>
        /// The overall trade rate at this event.
        /// </summary>
        public double TradeRate { get; set; }

        /// <summary>
        /// The Relative Strength Index (RSI) value at this event.
        /// </summary>
        public double RSI { get; set; }

        /// <summary>
        /// The active trading strategies applied at this event.
        /// </summary>
        public string Strategies { get; set; } = null!;

        /// <summary>
        /// The best bid price for the Yes side of the market.
        /// </summary>
        public int BestYesBid { get; set; }

        /// <summary>
        /// The best ask price for the Yes side of the market.
        /// </summary>
        public int BestYesAsk { get; set; }

        /// <summary>
        /// The best bid price for the No side of the market.
        /// </summary>
        public int BestNoBid { get; set; }

        /// <summary>
        /// The best ask price for the No side of the market.
        /// </summary>
        public int BestNoAsk { get; set; }

        /// <summary>
        /// The bid-ask spread for the No side of the market.
        /// </summary>
        public int NoSpread { get; set; }

        /// <summary>
        /// The depth of orders at the best Yes bid price.
        /// </summary>
        public int DepthAtBestYesBid { get; set; }

        /// <summary>
        /// The depth of orders at the best Yes ask price.
        /// </summary>
        public int DepthAtBestYesAsk { get; set; }

        /// <summary>
        /// The depth of orders at the best No bid price.
        /// </summary>
        public int DepthAtBestNoBid { get; set; }

        /// <summary>
        /// The depth of orders at the best No ask price.
        /// </summary>
        public int DepthAtBestNoAsk { get; set; }

        /// <summary>
        /// The total number of bid contracts on the Yes side.
        /// </summary>
        public int TotalYesBidContracts { get; set; }

        /// <summary>
        /// The total number of bid contracts on the No side.
        /// </summary>
        public int TotalNoBidContracts { get; set; }

        /// <summary>
        /// The imbalance between Yes and No bid volumes.
        /// </summary>
        public int BidImbalance { get; set; }

        /// <summary>
        /// The trade rate per minute for the Yes side.
        /// </summary>
        public double TradeRatePerMinute_Yes { get; set; }

        /// <summary>
        /// The trade rate per minute for the No side.
        /// </summary>
        public double TradeRatePerMinute_No { get; set; }

        /// <summary>
        /// The trade volume per minute for the Yes side.
        /// </summary>
        public double TradeVolumePerMinute_Yes { get; set; }

        /// <summary>
        /// The trade volume per minute for the No side.
        /// </summary>
        public double TradeVolumePerMinute_No { get; set; }

        /// <summary>
        /// The number of trades for the Yes side.
        /// </summary>
        public int TradeCount_Yes { get; set; }

        /// <summary>
        /// The number of trades for the No side.
        /// </summary>
        public int TradeCount_No { get; set; }

        /// <summary>
        /// The average trade size for the Yes side.
        /// </summary>
        public double AverageTradeSize_Yes { get; set; }

        /// <summary>
        /// The average trade size for the No side.
        /// </summary>
        public double AverageTradeSize_No { get; set; }

        /// <summary>
        /// Summary of resting bid orders on the Yes side, formatted as "price:quantity, price:quantity".
        /// </summary>
        public string RestingYesBids { get; set; } = "";

        /// <summary>
        /// Summary of resting bid orders on the No side, formatted as "price:quantity, price:quantity".
        /// </summary>
        public string RestingNoBids { get; set; } = "";

        /// <summary>
        /// Additional memo or notes for this event.
        /// </summary>
        public string Memo { get; set; } = "";

        /// <summary>
        /// The average cost of the current position.
        /// </summary>
        public double AverageCost { get; set; } = 0.0;

        /// <summary>
        /// List of detected technical patterns for this event snapshot.
        /// </summary>
        public List<BacklashPatterns.PatternDefinitions.PatternDefinition> Patterns { get; set; } = new List<BacklashPatterns.PatternDefinitions.PatternDefinition>();
    }
}