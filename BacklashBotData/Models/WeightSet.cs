namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a set of trading weights for portfolio optimization and strategy allocation.
    /// This entity defines how trading capital and resources should be distributed across
    /// different markets and strategies. Weight sets enable sophisticated portfolio management
    /// by allowing dynamic allocation based on market conditions, risk parameters, and
    /// performance metrics.
    /// </summary>
    public class WeightSet
    {
        /// <summary>
        /// Gets or sets the unique identifier for this weight set.
        /// This serves as the primary key in the database.
        /// </summary>
        public int WeightSetID { get; set; }

        /// <summary>
        /// Gets or sets the name of the strategy or portfolio that this weight set represents.
        /// This provides a human-readable identifier for the weight configuration.
        /// </summary>
        public string StrategyName { get; set; }

        /// <summary>
        /// Gets or sets the JSON string containing the weight allocation data.
        /// This defines how capital and resources are distributed across different markets or strategies.
        /// </summary>
        public string Weights { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this weight set was last executed or applied.
        /// This tracks when the weight allocation was most recently used in trading.
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Gets or sets the collection of market-specific weights within this weight set.
        /// This provides detailed allocation information for individual markets.
        /// </summary>
        public virtual ICollection<WeightSetMarket> WeightSetMarkets { get; set; } = new List<WeightSetMarket>();
    }
}
