namespace BacklashDTOs
{
    /// <summary>
    /// Represents a support or resistance level in trading.
    /// </summary>
    public class SupportResistanceLevel
    {
        /// <summary>
        /// Gets or sets the price level.
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// Gets or sets the number of times the level has been tested.
        /// </summary>
        public int TestCount { get; set; } // Number of times tested
        /// <summary>
        /// Gets or sets the cumulative volume at this level.
        /// </summary>
        public long TotalVolume { get; set; } // Cumulative volume at this level
        /// <summary>
        /// Gets or sets the number of candlesticks contributing to this level.
        /// </summary>
        public int CandlestickCount { get; set; } // Number of candlesticks contributing
        /// <summary>
        /// Gets or sets the strength of the level.
        /// </summary>
        public double Strength { get; set; }
    }
}
