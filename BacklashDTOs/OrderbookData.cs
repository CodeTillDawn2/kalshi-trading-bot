namespace BacklashDTOs
{
    /// <summary>
    /// Represents orderbook data for a specific market, focused on orderbook entries.
    /// </summary>
    public class OrderbookData
    {
        private string _marketTicker;

        /// <summary>
        /// Initializes orderbook data with a market ticker, orderbook, and optional MarketData reference.
        /// </summary>
        public OrderbookData(string marketTicker, int price, string side, int resting_contracts)
        {
            _marketTicker = marketTicker;
            Price = price;
            Side = side;
            RestingContracts = resting_contracts;
            LastModifiedDate = DateTime.UtcNow;
            RefreshOrderbookMetadata();
        }

        /// <summary>
        /// Refreshes metadata based on orderbook data (placeholder).
        /// </summary>
        private void RefreshOrderbookMetadata()
        {
            // Placeholder for orderbook metadata refresh logic
            // Could sync with MarketData if needed
        }

        /// <summary>
        /// Gets or sets the market ticker.
        /// </summary>
        public string MarketTicker
        {
            get => _marketTicker;
            set => _marketTicker = value;
        }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public int Price { get; set; }
        /// <summary>
        /// Gets or sets the side.
        /// </summary>
        public string Side { get; set; }
        /// <summary>
        /// Gets or sets the resting contracts.
        /// </summary>
        public int RestingContracts { get; set; }
        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }


    }
}
