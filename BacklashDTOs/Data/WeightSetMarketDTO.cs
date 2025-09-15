namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for weight set market data.
    /// </summary>
    public class WeightSetMarketDTO
    {
        /// <summary>
        /// Gets or sets the weight set identifier.
        /// </summary>
        public int WeightSetID { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public required string MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the profit and loss value.
        /// </summary>
        public decimal PnL { get; set; }

        /// <summary>
        /// Gets or sets the last run timestamp.
        /// </summary>
        public DateTime? LastRun { get; set; }
    }
}
