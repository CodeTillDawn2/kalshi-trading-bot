using System.Text.Json;

namespace BacklashDTOs
{
    public class OrderBookEventArgs : EventArgs
    {
        public string OfferType { get; }
        public JsonElement Data { get; }
        public OrderBookEventArgs(string offerType, JsonElement data)
            => (OfferType, Data) = (offerType, data);
    }

    public class TickerEventArgs : EventArgs
    {
        public Guid market_id { get; set; }
        public string market_ticker { get; set; }
        public int price { get; set; }
        public int yes_bid { get; set; }
        public int yes_ask { get; set; }
        public int volume { get; set; }
        public int open_interest { get; set; }
        public int dollar_volume { get; set; }
        public int dollar_open_interest { get; set; }
        public long ts { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }

    public class TradeEventArgs : EventArgs
    {
        public JsonElement Data { get; }
        public TradeEventArgs(JsonElement data) => Data = data;
    }

    public class FillEventArgs : EventArgs
    {
        public JsonElement Data { get; }
        public FillEventArgs(JsonElement data) => Data = data;
    }

    public class MarketLifecycleEventArgs : EventArgs
    {
        public JsonElement Data { get; }
        public MarketLifecycleEventArgs(JsonElement data) => Data = data;
    }

    public class EventLifecycleEventArgs : EventArgs
    {
        public JsonElement Data { get; }
        public EventLifecycleEventArgs(JsonElement data) => Data = data;
    }
}
