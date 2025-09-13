using TradingSimulator.Strategies;

namespace TradingSimulator
{
    /// <summary>
    /// Represents a point in time with a price value and optional memo for market data visualization.
    /// This record is used throughout the trading simulator to store timestamped price information
    /// with associated metadata for charting and analysis purposes.
    /// </summary>
    public record PricePoint(DateTime Date, double Price, string? Memo = null)
    {
        /// <summary>
        /// Gets the memo split into individual parts, trimmed and filtered for empty entries.
        /// This property provides easy access to parsed memo components for analysis.
        /// </summary>
        public List<string> MemoParts => string.IsNullOrEmpty(Memo)
            ? new List<string>()
            : Memo.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(part => part.Trim())
                  .Where(part => !string.IsNullOrEmpty(part))
                  .ToList();
    }

    /// <summary>
    /// Represents a trade point with timestamp, price, and trading decision.
    /// This record encapsulates the essential data for a trading action in the simulator.
    /// </summary>
    public record TradePoint(DateTime Date, double Price, TradingDecisionEnum Decision);

    /// <summary>
    /// Contains cached market data for a specific market, including profit/loss, position information,
    /// and various lists of price points representing different market events and states.
    /// This class serves as the primary data structure for storing processed market simulation results
    /// that can be serialized to JSON for persistence and later visualization in the GUI.
    /// </summary>
    public class CachedMarketData
    {
        /// <summary>
        /// Gets or sets the market ticker symbol this cached data represents.
        /// </summary>
        public string Market { get; set; }

        /// <summary>
        /// Gets or sets the final profit and loss from the market simulation.
        /// </summary>
        public double PnL { get; set; }

        /// <summary>
        /// Gets or sets the final simulated position at the end of the market processing.
        /// </summary>
        public int SimulatedPosition { get; set; }

        /// <summary>
        /// Gets or sets the average cost of the position.
        /// </summary>
        public double AverageCost { get; set; }

        /// <summary>
        /// Gets or sets the list of bid price points over time.
        /// </summary>
        public List<PricePoint> BidPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of ask price points over time.
        /// </summary>
        public List<PricePoint> AskPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of buy trade points where positions were entered.
        /// </summary>
        public List<PricePoint> BuyPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of sell trade points where positions were exited.
        /// </summary>
        public List<PricePoint> SellPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of exit points where positions were closed.
        /// </summary>
        public List<PricePoint> ExitPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of event points marking significant market events.
        /// </summary>
        public List<PricePoint> EventPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where long positions were intended but not executed.
        /// </summary>
        public List<PricePoint> IntendedLongPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where short positions were intended but not executed.
        /// </summary>
        public List<PricePoint> IntendedShortPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of position size points over time.
        /// </summary>
        public List<PricePoint> PositionPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of average cost points over time.
        /// </summary>
        public List<PricePoint> AverageCostPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of resting orders count points over time.
        /// </summary>
        public List<PricePoint> RestingOrdersPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where orderbook discrepancies were detected.
        /// </summary>
        public List<PricePoint> DiscrepancyPoints { get; set; } = new List<PricePoint>();

        /// <summary>
        /// Gets or sets the list of points where candlestick patterns were detected.
        /// </summary>
        public List<PricePoint> PatternPoints { get; set; } = new List<PricePoint>();
    }

}
