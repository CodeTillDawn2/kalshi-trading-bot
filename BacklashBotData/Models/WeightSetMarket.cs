
namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents the market-specific allocation and performance data within a weight set.
    /// This entity defines how individual markets are weighted within a portfolio strategy,
    /// tracking both the allocation percentage and the realized performance. It enables
    /// granular control over portfolio composition and performance attribution at the
    /// market level.
    /// </summary>
    public class WeightSetMarket
    {
        /// <summary>
        /// Gets or sets the foreign key reference to the parent weight set.
        /// This links the market allocation to its associated weight configuration.
        /// </summary>
        public int WeightSetID { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol for this allocation.
        /// This identifies the specific market contract included in the weight set.
        /// </summary>
        public required string MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the profit and loss for this market within the weight set.
        /// This tracks the performance contribution of this specific market allocation.
        /// </summary>
        public decimal PnL { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this market allocation was last executed.
        /// This tracks when the weight was most recently applied to this market.
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated WeightSet entity.
        /// This provides access to the parent weight configuration and related allocations.
        /// </summary>
        public virtual WeightSet? WeightSet { get; set; }
    }
}
