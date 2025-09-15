namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for ticker data.
    /// </summary>
    public class TickerDTO
    {
        /// <summary>
        /// Gets or sets the market identifier.
        /// </summary>
        public Guid market_id { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the current price.
        /// </summary>
        public int price { get; set; }

        /// <summary>
        /// Gets or sets the yes bid price.
        /// </summary>
        public int yes_bid { get; set; }

        /// <summary>
        /// Gets or sets the yes ask price.
        /// </summary>
        public int yes_ask { get; set; }

        /// <summary>
        /// Gets or sets the trading volume.
        /// </summary>
        public int volume { get; set; }

        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        public int open_interest { get; set; }

        /// <summary>
        /// Gets or sets the dollar volume.
        /// </summary>
        public int dollar_volume { get; set; }

        /// <summary>
        /// Gets or sets the dollar open interest.
        /// </summary>
        public int dollar_open_interest { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public long ts { get; set; }

        /// <summary>
        /// Gets or sets the logged date.
        /// </summary>
        public DateTime LoggedDate { get; set; }

        /// <summary>
        /// Gets or sets the processed date.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }
    }
}
