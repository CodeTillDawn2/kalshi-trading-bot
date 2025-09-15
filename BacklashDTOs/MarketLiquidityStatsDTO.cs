namespace BacklashDTOs
{
    /// <summary>
    /// Represents market liquidity statistics.
    /// </summary>
    public class MarketLiquidityStatsDTO
    {
        /// <summary>
        /// Gets or sets the 24-hour volume.
        /// </summary>
        public int volume_24h { get; set; }
        /// <summary>
        /// Gets or sets the liquidity value.
        /// </summary>
        public long liquidity { get; set; }
        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        public int open_interest { get; set; }
        /// <summary>
        /// Gets or sets the yes bid.
        /// </summary>
        public int yes_bid { get; set; }
        /// <summary>
        /// Gets or sets the no bid.
        /// </summary>
        public int no_bid { get; set; }


    }
}
