namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for weight set data.
    /// </summary>
    public class WeightSetDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the weight set.
        /// </summary>
        public int WeightSetID { get; set; }

        /// <summary>
        /// Gets or sets the strategy name.
        /// </summary>
        public string? StrategyName { get; set; }

        /// <summary>
        /// Gets or sets the weights data.
        /// </summary>
        public string? Weights { get; set; }

        /// <summary>
        /// Gets or sets the last run timestamp.
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Gets or sets the list of weight set markets.
        /// </summary>
        public List<WeightSetMarketDTO> WeightSetMarkets { get; set; } = new List<WeightSetMarketDTO>();
    }
}
