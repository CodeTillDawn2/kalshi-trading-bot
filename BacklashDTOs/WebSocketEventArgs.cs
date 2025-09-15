using System.Text.Json;

namespace BacklashDTOs
{
    /// <summary>
    /// Event arguments for order book events.
    /// </summary>
    public class OrderBookEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of offer.
        /// </summary>
        public string OfferType { get; }

        /// <summary>
        /// Gets the JSON data for the event.
        /// </summary>
        public JsonElement Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBookEventArgs"/> class.
        /// </summary>
        /// <param name="offerType">The type of offer.</param>
        /// <param name="data">The JSON data.</param>
        public OrderBookEventArgs(string offerType, JsonElement data)
            => (OfferType, Data) = (offerType, data);
    }

    /// <summary>
    /// Event arguments for ticker events.
    /// </summary>
    public class TickerEventArgs : EventArgs
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

    /// <summary>
    /// Event arguments for trade events.
    /// </summary>
    public class TradeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the JSON data for the trade event.
        /// </summary>
        public JsonElement Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeEventArgs"/> class.
        /// </summary>
        /// <param name="data">The JSON data.</param>
        public TradeEventArgs(JsonElement data) => Data = data;
    }

    /// <summary>
    /// Event arguments for fill events.
    /// </summary>
    public class FillEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the JSON data for the fill event.
        /// </summary>
        public JsonElement Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FillEventArgs"/> class.
        /// </summary>
        /// <param name="data">The JSON data.</param>
        public FillEventArgs(JsonElement data) => Data = data;
    }

    /// <summary>
    /// Event arguments for market lifecycle events.
    /// </summary>
    public class MarketLifecycleEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the JSON data for the market lifecycle event.
        /// </summary>
        public JsonElement Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketLifecycleEventArgs"/> class.
        /// </summary>
        /// <param name="data">The JSON data.</param>
        public MarketLifecycleEventArgs(JsonElement data) => Data = data;
    }

    /// <summary>
    /// Event arguments for event lifecycle events.
    /// </summary>
    public class EventLifecycleEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the JSON data for the event lifecycle event.
        /// </summary>
        public JsonElement Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLifecycleEventArgs"/> class.
        /// </summary>
        /// <param name="data">The JSON data.</param>
        public EventLifecycleEventArgs(JsonElement data) => Data = data;
    }
}
